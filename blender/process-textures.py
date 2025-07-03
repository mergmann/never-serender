import json
import os
import time

from argparse import ArgumentParser
from base64 import b64encode
from threading import Lock, Thread
from PIL import Image
from queue import Queue, ShutDown
from rich.console import Console, Group
from rich.live import Live
from rich.progress import BarColumn, Progress, SpinnerColumn, TaskID, TextColumn, TimeRemainingColumn, TransferSpeedColumn
from rich.status import Status
from rich.panel import Panel
from xxhash import xxh3_128

DEFAULT_THREADS = os.cpu_count() or 1
DEFAULT_RESIZE = 2
DEFAULT_THRESHOLD = 1024
DEFAULT_RESAMPLING = 'LANCZOS'

parser = ArgumentParser(description='Process DDS textures to PNG format.')
parser.add_argument('--input', type=str, required=True, help='Input directory containing DDS textures.')
parser.add_argument('--output', type=str, required=True, help='Output directory for PNG textures.')
parser.add_argument('--threads', type=int, default=DEFAULT_THREADS, help=f'Number of threads to use for processing. [default: {DEFAULT_THREADS}]')
parser.add_argument('--resize', type=int, default=DEFAULT_RESIZE, help=f'Resize factor for images. [default: {DEFAULT_RESIZE}]')
parser.add_argument('--threshold', type=int, default=DEFAULT_THRESHOLD, help=f'Threshold for resizing images. [default: {DEFAULT_THRESHOLD}]')
parser.add_argument('--resampling', type=str, default=DEFAULT_RESAMPLING, help=f'Resampling method for resizing. [default: {DEFAULT_RESAMPLING}]')

args = parser.parse_args()

INPUT_PATH: str = args.input
OUTPUT_PATH: str = args.output
THREADS: int = args.threads
RESIZE: int = args.resize
THRESHOLD: int = args.threshold
RESAMPLING: Image.Resampling = getattr(Image.Resampling, args.resampling.upper())

console = Console()

class AtomicCount:
    def __init__(self, initial: int = 0) -> None:
        self._lock = Lock()
        self._value = initial

    def increment(self):
        with self._lock:
            result = self._value
            self._value += 1
        return result

    def get(self) -> int:
        with self._lock:
            return self._value

class HashCache:
    def __init__(self):
        self._lock = Lock()
        self._in_hashes = dict[str, str]()
        self._out_hashes = dict[str, str]()

    def load(self, path: str):
        try:
            with open(path, 'rb') as f:
                data = json.load(f)
            with self._lock:
                self._in_hashes |= data.get("in", {})
                self._out_hashes |= data.get("out", {})
        except Exception as e:
            console.print(f'[bold red]Error loading cache from[/] [light_black]{path}[/]: [yellow]{e}[/]')

    def save(self, path: str):
        with self._lock:
            data = {
                "in": self._in_hashes,
                "out": self._out_hashes
            }
            try:
                with open(path, 'w') as f:
                    json.dump(data, f, indent=2)
            except Exception as e:
                console.print(f'[bold red]Error saving cache to[/] [light_black]{path}[/]: [yellow]{e}[/]')

    def get(self, path: str) -> tuple[str | None, str | None]:
        with self._lock:
            return (self._in_hashes.get(path, None), self._out_hashes.get(path, None))

    def set(self, key: str, value: tuple[str, str]):
        with self._lock:
            self._in_hashes[key] = value[0]
            self._out_hashes[key] = value[1]

def hash_file(path: str) -> str | None:
    try:
        hasher = xxh3_128()
        with open(path, 'rb') as f:
            while chunk := f.read(8192):
                hasher.update(chunk)
        return b64encode(hasher.digest()).decode('ascii')
    except Exception:
        return None

def convert(inpath: str, outpath: str):
    dirname = os.path.dirname(outpath)
    os.makedirs(dirname, exist_ok=True)
    img = Image.open(inpath)
    if RESIZE > 1:
        w, h = img.size
        if w > THRESHOLD and h > THRESHOLD:
            img = img.resize((w // RESIZE, h // RESIZE), RESAMPLING)
    img.save(outpath)

def convert_worker(index: int, items: Queue[tuple[str, str, int]], total: int, counter: AtomicCount, console: Console, progress: Progress, task: TaskID, status: Status):
    while True:
        try:
            path, in_hash, size = items.get()
        except ShutDown:
            break

        relpath = ''

        try:
            relpath = os.path.relpath(path, INPUT_PATH)
            file, _ = os.path.splitext(relpath)
            outpath = os.path.join(OUTPUT_PATH, file + '.png')

            value = counter.increment()
            status.update(f'[yellow]{index + 1:>2}[/] [bright_black]{relpath}[/]')
            progress.update(task, advance=size, description=f'\\[[blue]{value + 1}/{total}[/]]')

            convert(path, outpath)
            out_hash = hash_file(outpath)
            if out_hash is not None:
                cache.set(relpath, (in_hash, out_hash))
        except Exception as e:
            console.print(f'[bold red]Error converting[/] [bright_black]{relpath}[/]: [yellow]{e}[/]')
        finally:
            items.task_done()
    status.update(f'[yellow]{index + 1:>2}[/] [green]Done[/]')

def find_files(input_path: str) -> tuple[list[tuple[str, int]], int]:
    paths = list[tuple[str, int]]()
    total_size = 0

    for dirpath, _, files in os.walk(input_path):
        for file in files:
            path = os.path.join(dirpath, file)
            if not path.lower().endswith('.dds'):
                continue

            size = os.path.getsize(path)
            paths.append((path, size))
            total_size += size

    return paths, total_size

def scan_files(paths: list[tuple[str, int]], input_path: str, output_path: str, progress: Progress, task: TaskID) -> tuple[list[tuple[str, str, int]], int]:
    total_size = 0
    to_process = list[tuple[str, str, int]]()

    for path, size in paths:
        if not path.lower().endswith('.dds'):
            continue

        relpath = os.path.relpath(path, input_path)
        file, _ = os.path.splitext(relpath)
        outpath = os.path.join(output_path, file + '.png')

        needs_update = True

        in_hash, out_hash = cache.get(relpath)
        in_hash_cmp = hash_file(path)
        if in_hash_cmp == in_hash:
            if out_hash is not None:
                out_hash_cmp = hash_file(outpath)
                if out_hash_cmp == out_hash:
                    # File is already processed and cached
                    needs_update = False
    
        if needs_update and in_hash_cmp is not None:
            to_process.append((path, in_hash_cmp, size))
            total_size += size
        progress.update(task, advance=size)

    progress.update(task, completed=True)

    return to_process, total_size

if __name__ == '__main__':
    cache = HashCache()
    cache.load(os.path.join(OUTPUT_PATH, 'hashes.json'))

    with Status('[green]Finding files...[/]', console=console) as status:
        paths, paths_size = find_files(INPUT_PATH)

    progress = Progress(
        SpinnerColumn(),
        TextColumn('{task.description}'),
        BarColumn(),
        TimeRemainingColumn(),
        TransferSpeedColumn(),
        console=console,
    )
    with progress:
        task = progress.add_task('[green]Finding changes...[/]', total=paths_size)
        to_process, total_size = scan_files(paths, INPUT_PATH, OUTPUT_PATH, progress, task)

    total = len(to_process)
    queue = Queue()

    counter = AtomicCount()

    statuses = []
    progress = Progress(
        SpinnerColumn(),
        TextColumn('{task.description}'),
        BarColumn(),
        TimeRemainingColumn(),
        TransferSpeedColumn(),
        console=console,
    )
    task = progress.add_task("[green]Processing...", total=total_size)

    for index in range(THREADS):
        status = Status(f'[orange]{index + 1:<2}[/] [blue]Pending[/]', console=console)
        statuses.append(status)

        thread = Thread(target=convert_worker, args=(index, queue, total, counter, console, progress, task, status))
        thread.start()

    try:
        with Live(Panel(Group(*statuses, progress)), console=console):
            start = time.time()
            for item in to_process:
                queue.put(item)
            queue.shutdown()
            queue.join()
            end = time.time()

        console.print(f'\n[green]Done![/] Processed {total} files in {end - start:.2f} seconds.')
    finally:
        os.makedirs(OUTPUT_PATH, exist_ok=True)
    
        cache.save(os.path.join(OUTPUT_PATH, 'hashes.json'))
        console.print(f'[blue]Cache saved to {os.path.join(OUTPUT_PATH, "hashes.json")}[/]')
