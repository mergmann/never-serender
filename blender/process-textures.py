from multiprocessing import Value
from multiprocessing.pool import ThreadPool
import os
from PIL import Image

INPUT_PATH = '../../../spacemodel/DDSTextures/'
OUTPUT_PATH = '../../../spacemodel/Textures/'

THREADS = None

RESIZE = 1 # Resize factor for images, if needed
THRESHOLD = 1024 # Threshold for resizing images
RESAMPLING = Image.Resampling.LANCZOS  # Resampling method for resizing

class AtomicCounter:
    def __init__(self, initial=0):
        self.value = Value('i', initial)
    
    def increment(self):
        with self.value.get_lock():
            self.value.value += 1
    
    def get(self):
        with self.value.get_lock():
            return self.value.value

def convert(path: str):
    relpath = os.path.relpath(path, INPUT_PATH)
    file, _ = os.path.splitext(relpath)
    outpath = os.path.join(OUTPUT_PATH, file + '.png')
    try:
        if not os.path.exists(outpath):
            dirname = os.path.dirname(outpath)
            os.makedirs(dirname, exist_ok=True)
            img = Image.open(path)
            if RESIZE > 1:
                w, h = img.size
                if w > THRESHOLD and h > THRESHOLD:
                    img = img.resize((w // RESIZE, h // RESIZE), RESAMPLING)
                    img.save(outpath)
    except Exception as e:
        print(f'Error converting {relpath}: {e}')
    done.increment()
    print(f'[{done.get()}/{total}] {relpath}')


paths = []
for dirpath, dirs, files in os.walk(INPUT_PATH):
    for file in files:
        path = os.path.join(dirpath, file)
        if path.lower().endswith('.dds'):
            paths.append(path)

total = len(paths)
done = AtomicCounter()

pool = ThreadPool(THREADS)
pool.map(convert, paths)
