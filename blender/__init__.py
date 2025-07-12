from __future__ import annotations

import math
import os
import struct
import time
import typing
import bpy
import bpy_extras as bpx

from dataclasses import dataclass
from enum import Enum
from io import SEEK_CUR, SEEK_END
from mathutils import Matrix
from typing import IO, Any, Callable, Generic, Iterable, Self, TypeVar
from bpy_extras.wm_utils.progress_report import ProgressReport,  ProgressReportSubstep

Vec3i = tuple[int, int, int]

Vec3i_Zero = (0, 0, 0)

Vec2 = tuple[float, float]
Vec3 = tuple[float, float, float]
Vec4 = tuple[float, float, float, float]

Vec3_Zero = (0.0, 0.0, 0.0)

Mat4 = tuple[Vec4, Vec4, Vec4, Vec4]
Mat4_Identity = (
    (1.0, 0.0, 0.0, 0.0),
    (0.0, 1.0, 0.0, 0.0),
    (0.0, 0.0, 1.0, 0.0),
    (0.0, 0.0, 0.0, 1.0)
)

Color_Default = (1.0, 1.0, 1.0)
ColorMask_Default = (0.0, 0.0, 0.0)

_T = TypeVar('_T')
_TE = TypeVar('_TE', bound='Event')
_D = TypeVar('_D')

NODE_SETEX = 'nSEr SETex'
FPS = 60
GLASS_HACK = False
"""
Enable this if the glass refracts too much
"""

class PropertyType(Generic[_T]):
    def __init__(self, magic: int, name: str, read: Callable[[BinReader], _T]) -> None:
        self.magic = magic
        self.name = name
        self.read = read

    def __str__(self) -> str:
        return f'PropertyType({self.name})'

    def __repr__(self) -> str:
        return f'PropertyType({self.magic:04X}, {self.name})'

class EventType(Generic[_TE]):
    def __init__(self, magic: int, name: str, read: Callable[[int, Properties, BinReader], _TE]) -> None:
        self.magic = magic
        self.name = name
        self.read = read

    def __str__(self) -> str:
        return f'EventType({self.name})'
    
    def __repr__(self) -> str:
        return f'EventType({self.magic:04X}, {self.name})'

class TextureType(Enum):
    Auto = 0x00
    PNG = 0x01
    DDS = 0x02

class TextureKind(Enum):
    ColorMetal  = 0x00
    NormalGloss = 0x01
    AddMaps     = 0x02
    AlphaMask   = 0x03

class RenderMode(Enum):
    Normal = 0x00
    Glass = 0x01

class Direction(Enum):
    Forward  = 0
    Backward = 1
    Left     = 2
    Right    = 3
    Up       = 4
    Down     = 5

    def vector(self) -> Vec3i:
        match self:
            case Direction.Forward:  return ( 0,  0, -1)
            case Direction.Backward: return ( 0,  0,  1)
            case Direction.Left:     return (-1,  0,  0)
            case Direction.Right:    return ( 1,  0,  0)
            case Direction.Up:       return ( 0,  1,  0)
            case Direction.Down:     return ( 0, -1,  0)

@dataclass
class BlockOrientation:
    forward: Direction
    up:      Direction
    right:   Direction

    @classmethod
    def from_u8(cls, value: int) -> BlockOrientation:
        return BlockOrientation(
            Direction(value % 6),
            Direction((value // 6) % 6),
            Direction((value // 36) % 6),
        )

@dataclass
class MeshInfo:
    tri_start: int
    tri_count: int
    mat_id:    int

@dataclass
class MaterialOverride:
    src_id: int
    dst_id: int

def unpack_color_mask(mask: Vec3i) -> Vec3:
    hb, sb, vb = mask
    return (
        hb / 255.0,
        (sb / 127.5) - 1.0,
        (vb / 127.5) - 1.0
    )

class PropertyTypes:
    EndHeader    = PropertyType[None]                   (0x0000, 'EndHeader',    lambda r: None)
    Id           = PropertyType[int]                    (0x0108, 'Id',           lambda r: r.i64())
    Name         = PropertyType[str]                    (0x02FF, 'Name',         lambda r: r.string())
    Author       = PropertyType[str]                    (0x03FF, 'Author',       lambda r: r.string())
    Path         = PropertyType[str]                    (0x04FF, 'Path',         lambda r: r.string())
    Matrix       = PropertyType[Mat4]                   (0x0540, 'Matrix',       lambda r: r.mat4f())
    MatrixD      = PropertyType[Mat4]                   (0x0580, 'MatrixD',      lambda r: r.mat4d())
    TextureType  = PropertyType[TextureType]            (0x0601, 'TextureType',  lambda r: TextureType(r.u8()))
    Vertices     = PropertyType[list[Vec3]]             (0x07FF, 'Vertices',     lambda r: r.sized().all(r.vec3f))
    Normals      = PropertyType[list[Vec3]]             (0x08FF, 'Normals',      lambda r: r.sized().all(r.vec3f))
    TexCoords    = PropertyType[list[Vec2]]             (0x09FF, 'TexCoords',    lambda r: r.sized().all(r.vec2f))
    Indices      = PropertyType[list[Vec3i]]            (0x0AFF, 'Indices',      lambda r: r.sized().all(r.vec3i))
    Meshes       = PropertyType[list[MeshInfo]]         (0x0BFF, 'Meshes',       lambda r: r.sized().all(r.mesh))
    MaterialMods = PropertyType[list[MaterialOverride]] (0x0CFF, 'MaterialMods', lambda r: r.sized().all(r.mat_override))
    Model        = PropertyType[int]                    (0x0D04, 'Model',        lambda r: r.u32())
    Color        = PropertyType[Vec3]                   (0x0E0C, 'Color',        lambda r: r.vec3f())
    ColorMask    = PropertyType[Vec3]                   (0x0E03, 'ColorMask',    lambda r: unpack_color_mask(r.vec3b()))
    Delta        = PropertyType[float]                  (0x0F04, 'Delta',        lambda r: r.f32())
    Cone         = PropertyType[Vec2]                   (0x1008, 'Cone',         lambda r: r.vec2f())
    Scale        = PropertyType[float]                  (0x1104, 'Scale',        lambda r: r.f32())
    Remove       = PropertyType[None]                   (0x1200, 'Remove',       lambda r: None)
    Preview      = PropertyType[bool]                   (0x1301, 'Preview',      lambda r: r.bool())
    Parent       = PropertyType[int]                    (0x1404, 'Parent',       lambda r: r.u32())
    Show         = PropertyType[bool]                   (0x1501, 'Show',         lambda r: r.bool())
    RenderMode   = PropertyType[RenderMode]             (0x1601, 'RenderMode',   lambda r: RenderMode(r.u8()))
    Texture      = PropertyType[tuple[TextureKind, int]](0x1705, 'Texture',      lambda r: (TextureKind(r.u8()), r.u32()))
    Vector3      = PropertyType[Vec3]                   (0x180C, 'Vector3',      lambda r: r.vec3f())
    Vector3S     = PropertyType[Vec3i]                  (0x1806, 'Vector3S',     lambda r: r.vec3s())
    Orientation  = PropertyType[BlockOrientation]       (0x1901, 'Orientation',  lambda r: BlockOrientation.from_u8(r.u8()))

PropertyTypeMap = dict[int, PropertyType[Any]]()
for attr in dir(PropertyTypes):
    if not attr.startswith('_'):
        prop = getattr(PropertyTypes, attr)
        if isinstance(prop, PropertyType):
            PropertyTypeMap[prop.magic] = prop

class Properties:
    def __init__(self) -> None:
        self.data = dict[PropertyType[Any], list[Any]]()

    def add(self, key: PropertyType[Any], value: Any) -> None:
        if key in self.data:
            self.data[key].append(value)
        else:
            self.data[key] = [value]

    def get(self, key: PropertyType[_T], default: _D = None) -> _T | _D:
        return self.data.get(key, [default])[0]

    def pop(self, key: PropertyType[_T], default: _D = None) -> _T | _D:
        return self.data.pop(key, [default])[0]

    def get_all(self, key: PropertyType[_T]) -> list[_T]:
        return self.data.get(key, [])

    def pop_all(self, key: PropertyType[_T]) -> list[_T]:
        return self.data.pop(key, [])

    def __contains__(self, key: PropertyType[Any]) -> bool:
        return key in self.data
    
    def __str__(self) -> str:
        return str(self.data)
    
    def __repr__(self) -> str:
        return f'Properties({self.data!r})'

@dataclass
class Event:
    id:    int
    props: Properties

@dataclass
class BlockEvent(Event):
    parent:      int
    position:    Vec3i
    translation: Vec3
    orientation: BlockOrientation
    color:       Vec3
    entity:      int | None
    name:        str | None
    model:       int | None
    overrides:   list[MaterialOverride]
    remove:      bool

    @classmethod
    def read(cls, id: int, props: Properties, r: BinReader) -> Self:
        return cls(
            id,
            props,
            props.pop(PropertyTypes.Parent, -1),
            props.pop(PropertyTypes.Vector3S, Vec3i_Zero),
            props.pop(PropertyTypes.Vector3, Vec3_Zero),
            props.pop(PropertyTypes.Orientation, BlockOrientation.from_u8(0)),
            props.pop(PropertyTypes.ColorMask, ColorMask_Default),
            props.pop(PropertyTypes.Id, None),
            props.pop(PropertyTypes.Name, None),
            props.pop(PropertyTypes.Model, None),
            props.pop(PropertyTypes.MaterialMods, []),
            PropertyTypes.Remove in props,
        )

@dataclass
class EndEvent(Event):
    @classmethod
    def read(cls, id: int, props: Properties, r: BinReader) -> Self:
        return cls(id, props)

@dataclass
class AdvanceEvent(Event):
    delta: float

    @classmethod
    def read(cls, id: int, props: Properties, r: BinReader) -> Self:
        return cls(
            id,
            props,
            props.pop(PropertyTypes.Delta, 0.0)
        )

@dataclass
class LightEvent(Event):
    matrix: Mat4
    color:  Vec3
    cone:   Vec2 | None

    @classmethod
    def read(cls, id: int, props: Properties, r: BinReader) -> Self:
        return cls(
            id,
            props,
            props.pop(PropertyTypes.MatrixD, Mat4_Identity),
            props.pop(PropertyTypes.Color, Color_Default),
            props.pop(PropertyTypes.Cone, None),
        )

@dataclass
class EntityEvent(Event):
    entity:  int
    parent:  int | None
    name:    str | None
    lmatrix: Mat4 | None
    wmatrix: Mat4 | None
    model:   int | None
    color:   Vec3 | None
    preview: bool | None
    show:    bool | None
    remove:  bool

    @classmethod
    def read(cls, id: int, props: Properties, r: BinReader) -> Self:
        return cls(
            id,
            props,
            props.pop(PropertyTypes.Id, -1),
            props.pop(PropertyTypes.Parent, None),
            props.pop(PropertyTypes.Name, None),
            props.pop(PropertyTypes.Matrix, None),
            props.pop(PropertyTypes.MatrixD, None),
            props.pop(PropertyTypes.Model, None),
            props.get(PropertyTypes.ColorMask, None),
            props.get(PropertyTypes.Preview, None),
            props.get(PropertyTypes.Show, None),
            PropertyTypes.Remove in props,
        )

@dataclass
class ModelEvent(Event):
    name:       str
    vertices:   list[Vec3]
    normals:    list[Vec3]
    tex_coords: list[Vec2]
    indices:    list[Vec3i]
    meshes:     list[MeshInfo]

    @classmethod
    def read(cls, id: int, props: Properties, r: BinReader) -> Self:
        return cls(
            id,
            props,
            props.pop(PropertyTypes.Name, 'unknown'),
            props.pop(PropertyTypes.Vertices, []),
            props.pop(PropertyTypes.Normals, []),
            props.pop(PropertyTypes.TexCoords, []),
            props.pop(PropertyTypes.Indices, []),
            props.pop(PropertyTypes.Meshes, []),
        )

@dataclass
class MaterialEvent(Event):
    name:         str
    render_mode:  RenderMode
    textures:     dict[TextureKind, int]

    @classmethod
    def read(cls, id: int, props: Properties, r: BinReader) -> Self:
        return cls(
            id,
            props,
            props.pop(PropertyTypes.Name, 'unknown'),
            props.pop(PropertyTypes.RenderMode, RenderMode.Normal),
            dict(props.pop_all(PropertyTypes.Texture)),
        )
    
    def merge(self, other: MaterialEvent) -> MaterialEvent:
        return MaterialEvent(
            self.id,
            self.props,
            f'{other.name}+{self.name}',
            self.render_mode,
            self.textures | other.textures
        )

@dataclass
class TextureEvent(Event):
    ty:   TextureType
    name: str
    path: str | None
    data: bytes | None

    @classmethod
    def read(cls, id: int, props: Properties, r: BinReader) -> Self:
        data = r.rest()
        return cls(
            id,
            props,
            props.pop(PropertyTypes.TextureType, TextureType.Auto),
            props.pop(PropertyTypes.Name, 'unknown'),
            props.pop(PropertyTypes.Path, None),
            data if len(data) > 0 else None,
        )

class EventTypes:
    End      = EventType[EndEvent]     (0x0000, 'End',      EndEvent.read)
    Advance  = EventType[AdvanceEvent] (0x0010, 'Advance',  AdvanceEvent.read)
    Texture  = EventType[TextureEvent] (0x0020, 'Texture',  TextureEvent.read)
    Material = EventType[MaterialEvent](0x0030, 'Material', MaterialEvent.read)
    Model    = EventType[ModelEvent]   (0x0040, 'Model',    ModelEvent.read)
    Entity   = EventType[EntityEvent]  (0x0050, 'Entity',   EntityEvent.read)
    Block    = EventType[BlockEvent]   (0x0051, 'Block',    BlockEvent.read)
    Light    = EventType[LightEvent]   (0x0060, 'Light',    LightEvent.read)

EventTypeMap = dict[int, EventType[Any]]()
for attr in dir(EventTypes):
    if not attr.startswith('_'):
        event_type = getattr(EventTypes, attr)
        if isinstance(event_type, EventType):
            EventTypeMap[event_type.magic] = event_type

class BinReader:
    def __init__(self, io: IO[bytes], end: int | None = None) -> None:
        self.io = io
        self.end = end

    def tell(self) -> int:
        return self.io.tell()
    
    def length(self) -> int:
        pos = self.io.tell()
        self.io.seek(0, SEEK_END)
        length = self.io.tell()
        self.io.seek(pos)
        return length

    def raw(self, n: int) -> bytes:
        buf = bytearray()
        if self.end is not None and self.io.tell() + n > self.end:
            raise ValueError('Attempting to read beyond end of constrained reader')
        while len(buf) < n:
            data = self.io.read(n - len(buf))
            if len(data) == 0:
                raise EOFError('Unexpected end of data')
            buf.extend(data)
        return buf
    
    def u(self, n: int) -> int:
        return int.from_bytes(self.raw(n), 'big')

    def u8(self) -> int: return self.u(1)
    def u16(self) -> int: return self.u(2)
    def u32(self) -> int: return self.u(4)
    def u64(self) -> int: return self.u(8)

    def i(self, n: int) -> int:
        return int.from_bytes(self.raw(n), 'big', signed=True)

    def i8(self) -> int: return self.i(1)
    def i16(self) -> int: return self.i(2)
    def i32(self) -> int: return self.i(4)
    def i64(self) -> int: return self.i(8)
    
    def f32(self) -> float: return struct.unpack('>f', self.raw(4))[0]
    def f64(self) -> float: return struct.unpack('>d', self.raw(8))[0]

    def bool(self) -> bool: return self.u8() != 0

    def vec3b(self) -> Vec3i:
        return struct.unpack('>BBB', self.raw(3))

    def vec3s(self) -> Vec3i:
        return struct.unpack('>hhh', self.raw(6))

    def vec3i(self) -> Vec3i:
        return struct.unpack('>iii', self.raw(12))

    def vec2f(self) -> Vec2:
        return struct.unpack('>ff', self.raw(8))
    def vec3f(self) -> Vec3:
        return struct.unpack('>fff', self.raw(12))
    def vec4f(self) -> Vec4:
        return struct.unpack('>ffff', self.raw(16))

    def vec4d(self) -> Vec4:
        return struct.unpack('>dddd', self.raw(32))

    def mat4f(self) -> tuple[Vec4, Vec4, Vec4, Vec4]:
        return (self.vec4f(), self.vec4f(), self.vec4f(), self.vec4f())

    def mat4d(self) -> tuple[Vec4, Vec4, Vec4, Vec4]:
        return (self.vec4d(), self.vec4d(), self.vec4d(), self.vec4d())
    
    def string(self) -> str:
        return self.io.read(self.u32()).decode('utf-8')

    def mesh(self) -> MeshInfo:
        return MeshInfo(self.u32(), self.u32(), self.u32())

    def mat_override(self) -> MaterialOverride:
        return MaterialOverride(self.u32(), self.u32())
    
    def restrict(self, end: int) -> BinReader:
        if self.end is not None and end > self.end:
            raise ValueError('Cannot restrict to a larger end')
        return BinReader(self.io, end)
    
    def sized(self, size: int | None = None) -> BinReader:
        if size is None:
            size = self.u32()
        return self.restrict(self.io.tell() + size)

    def all(self, f: Callable[[], _T]) -> list[_T]:
        if self.end is None:
            raise ValueError('Cannot read all items without end')
        items = []
        while self.io.tell() < self.end:
            items.append(f())
        return items
    
    def rest(self) -> bytes:
        if self.end is None:
            raise ValueError('Cannot read rest without end')
        return self.io.read(self.end - self.io.tell())

    def property(self) -> tuple[PropertyType[Any] | None, Any]:
        val = self.u16()
        try:
            ty = PropertyTypeMap[val]
        except KeyError:
            size = val & 0x00FF
            if size == 0xFF: # Dynamic size
                size = self.u32()
            self.io.seek(size, SEEK_CUR)
            print(f'Skipping unknown property type {val:>04X}')
            return None, None
        
        return ty, ty.read(self)

    def properties(self) -> Properties:
        props = Properties()
        while True:
            ty, prop = self.property()
            if ty is None:
                continue
            if ty is PropertyTypes.EndHeader:
                break
            props.add(ty, prop)
        return props
    
    def event(self) -> tuple[EventType[Any] | None, Event | None]:
        if self.u16() != 0xC080:
            raise ValueError('Invalid magic number for event header')
        val = self.u16()
        try:
            ty = EventTypeMap[val]
        except KeyError:
            size = self.u32()
            self.io.seek(size, SEEK_CUR)
            print(f'Skipping unknown event type {val}')
            return None, None

        id = self.u32()
        
        size = self.u32()
        pos = self.io.tell()
        end = pos + size
        r = self.restrict(end)

        # print(f'Start ty={ty} size={size} pos={pos} end={end}')

        props = r.properties()

        event = ty.read(id, props, r)

        # print(f'End pos={self.io.tell()} end={end}')

        self.io.seek(end)
        return ty, event
    
    def events(self) -> Iterable[Event]:
        while True:
            ty, event = self.event()
            if ty is EventTypes.End:
                break
            if event is not None:
                yield event

VariantKey = tuple[RenderMode, int | None, int | None, int | None, int | None]

class Data:
    def __init__(self, collection: bpy.types.Collection, setex: bpy.types.ShaderNodeTree, view_matrix: Matrix) -> None:
        self.setex = setex
        self.view_matrix = view_matrix
        self.textures  = dict[int, bpy.types.Image]()
        self.materials = dict[int, MaterialEvent]()
        self.meshes    = dict[int, bpy.types.Mesh]()
        self.models    = dict[int, ModelEvent]()
        self.entities  = dict[int, bpy.types.Object]()
        self.lights    = dict[int, bpy.types.Object]()
        self.variants  = dict[VariantKey, bpy.types.Material]()
        self.overrides = dict[int, dict[int, int]]()
        self.colors    = dict[int, Vec3]()
        self.frame     = -1
        self.collection_entities = bpy.data.collections.new('Entities')
        self.collection_lights = bpy.data.collections.new('Lights')
        collection.children.link(self.collection_entities)
        collection.children.link(self.collection_lights)

def neg3(a: Vec3) -> Vec3:
    x, y, z = a
    return (-x, -y, -z)

def neg4(a: Vec4) -> Vec4:
    x, y, z, w = a
    return (-x, -y, -z, -w)

def swap_yz(a: Vec3) -> Vec3:
    x, y, z = a
    return (x, z, y)

def cross(a: Vec3, b: Vec3) -> Vec3:
    return (
        a[1] * b[2] - a[2] * b[1],
        a[2] * b[0] - a[0] * b[2],
        a[0] * b[1] - a[1] * b[0]
    )

YZ_MATRIX = Matrix((
    (1, 0, 0, 0),
    (0, 0, 1, 0),
    (0, 1, 0, 0),
    (0, 0, 0, 1),
))

def convert_matrix(m: Mat4) -> Matrix:
    matrix = Matrix(m)
    matrix.transpose()
    return matrix @ YZ_MATRIX

# def convert_matrix(m: Mat4) -> Matrix:
#     matrix = Matrix(m)
#     matrix.transpose()
#     return MATRIX @ matrix @ MATRIX

# def convert_matrix(m: Mat4) -> Matrix:
#     return MATRIX @ Matrix(m) @ MATRIX

def get_matrix(translation: Vec3, orientation: BlockOrientation) -> Matrix:
    uv = orientation.up.vector()

    bv = neg3(orientation.forward.vector())
    rv = cross(uv, bv)
    uv = cross(bv, rv)
    tv = translation

    matrix = YZ_MATRIX @ Matrix(((*rv, 0), (*uv, 0), (*bv, 0), (*tv, 1))).transposed() @ YZ_MATRIX

    return matrix

@typing.no_type_check
def gen_setex_node() -> bpy.types.ShaderNodeTree:
    print(f'Generating SETex node')

    # Implementation of the block coloring shader
    # This is based on the game shader PostprocessColorizeExportedTexture.hlsl
    # Nodes exported from blender with https://github.com/BrendanParmer/NodeToPython
    nser_setex = bpy.data.node_groups.new(type = 'ShaderNodeTree', name = "nSEr SETex")

    nser_setex.color_tag = 'COLOR'
    nser_setex.description = ""
    nser_setex.default_group_node_width = 140
    

    #nser_setex interface
    #Socket Color
    color_socket = nser_setex.interface.new_socket(name = "Color", in_out='OUTPUT', socket_type = 'NodeSocketColor')
    color_socket.default_value = (0.0, 0.0, 0.0, 1.0)
    color_socket.attribute_domain = 'POINT'

    #Socket Color
    color_socket_1 = nser_setex.interface.new_socket(name = "Color", in_out='INPUT', socket_type = 'NodeSocketColor')
    color_socket_1.default_value = (0.5, 0.5, 0.5, 1.0)
    color_socket_1.attribute_domain = 'POINT'
    color_socket_1.description = "Base Texture Color"

    #Socket Colorize
    colorize_socket = nser_setex.interface.new_socket(name = "Colorize", in_out='INPUT', socket_type = 'NodeSocketVector')
    colorize_socket.default_value = (0.0, 0.0, 0.0)
    colorize_socket.min_value = -1.0
    colorize_socket.max_value = 1.0
    colorize_socket.subtype = 'NONE'
    colorize_socket.attribute_domain = 'POINT'
    colorize_socket.description = "Coloring value"

    #Socket Coloring
    coloring_socket = nser_setex.interface.new_socket(name = "Coloring", in_out='INPUT', socket_type = 'NodeSocketFloat')
    coloring_socket.default_value = 0.0
    coloring_socket.min_value = 0.0
    coloring_socket.max_value = 1.0
    coloring_socket.subtype = 'NONE'
    coloring_socket.attribute_domain = 'POINT'
    coloring_socket.description = "Coloring factor"


    #initialize nser_setex nodes
    #node Group Input
    group_input = nser_setex.nodes.new("NodeGroupInput")
    group_input.name = "Group Input"

    #node Group Output
    group_output = nser_setex.nodes.new("NodeGroupOutput")
    group_output.name = "Group Output"
    group_output.is_active_output = True

    #node Separate XYZ.001
    separate_xyz_001 = nser_setex.nodes.new("ShaderNodeSeparateXYZ")
    separate_xyz_001.label = "15 hsvmask"
    separate_xyz_001.name = "Separate XYZ.001"

    #node Combine Color.001
    combine_color_001 = nser_setex.nodes.new("ShaderNodeCombineColor")
    combine_color_001.label = "20 coloringc"
    combine_color_001.name = "Combine Color.001"
    combine_color_001.mode = 'HSV'
    #Green
    combine_color_001.inputs[1].default_value = 1.0
    #Blue
    combine_color_001.inputs[2].default_value = 1.0

    #node Mix
    mix = nser_setex.nodes.new("ShaderNodeMix")
    mix.label = "20* hsv"
    mix.name = "Mix"
    mix.blend_type = 'MIX'
    mix.clamp_factor = True
    mix.clamp_result = False
    mix.data_type = 'RGBA'
    mix.factor_mode = 'UNIFORM'
    #A_Color
    mix.inputs[6].default_value = (1.0, 1.0, 1.0, 1.0)

    #node Vector Math.001
    vector_math_001 = nser_setex.nodes.new("ShaderNodeVectorMath")
    vector_math_001.label = "20 hsv"
    vector_math_001.name = "Vector Math.001"
    vector_math_001.operation = 'MULTIPLY'

    #node Separate Color.001
    separate_color_001 = nser_setex.nodes.new("ShaderNodeSeparateColor")
    separate_color_001.label = "20 hsv"
    separate_color_001.name = "Separate Color.001"
    separate_color_001.mode = 'HSV'

    #node Combine XYZ
    combine_xyz = nser_setex.nodes.new("ShaderNodeCombineXYZ")
    combine_xyz.label = "25 hsv"
    combine_xyz.name = "Combine XYZ"
    #X
    combine_xyz.inputs[0].default_value = 0.0

    #node Vector Math.003
    vector_math_003 = nser_setex.nodes.new("ShaderNodeVectorMath")
    vector_math_003.label = "26 fhsv"
    vector_math_003.name = "Vector Math.003"
    vector_math_003.operation = 'MULTIPLY_ADD'
    #Vector_001
    vector_math_003.inputs[1].default_value = (1.0, 1.0, 0.5)

    #node Math
    math = nser_setex.nodes.new("ShaderNodeMath")
    math.label = "29* gray2"
    math.name = "Math"
    math.operation = 'MULTIPLY_ADD'
    math.use_clamp = True
    #Value_001
    math.inputs[1].default_value = 10.0
    #Value_002
    math.inputs[2].default_value = 10.0

    #node Math.002
    math_002 = nser_setex.nodes.new("ShaderNodeMath")
    math_002.label = "29 gray2"
    math_002.name = "Math.002"
    math_002.operation = 'SUBTRACT'
    math_002.use_clamp = False
    #Value
    math_002.inputs[0].default_value = 1.0

    #node Math.003
    math_003 = nser_setex.nodes.new("ShaderNodeMath")
    math_003.label = "30* fhsv.y"
    math_003.name = "Math.003"
    math_003.operation = 'ADD'
    math_003.use_clamp = True

    #node Math.004
    math_004 = nser_setex.nodes.new("ShaderNodeMath")
    math_004.label = "30* fhsv.z"
    math_004.name = "Math.004"
    math_004.operation = 'ADD'
    math_004.use_clamp = True

    #node Mix.001
    mix_001 = nser_setex.nodes.new("ShaderNodeMix")
    mix_001.label = "30 fhsv.y"
    mix_001.name = "Mix.001"
    mix_001.blend_type = 'MIX'
    mix_001.clamp_factor = True
    mix_001.clamp_result = False
    mix_001.data_type = 'FLOAT'
    mix_001.factor_mode = 'UNIFORM'

    #node Separate XYZ.002
    separate_xyz_002 = nser_setex.nodes.new("ShaderNodeSeparateXYZ")
    separate_xyz_002.label = "26 fhsv"
    separate_xyz_002.name = "Separate XYZ.002"

    #node Mix.002
    mix_002 = nser_setex.nodes.new("ShaderNodeMix")
    mix_002.label = "30 fhsv.z"
    mix_002.name = "Mix.002"
    mix_002.blend_type = 'MIX'
    mix_002.clamp_factor = True
    mix_002.clamp_result = False
    mix_002.data_type = 'FLOAT'
    mix_002.factor_mode = 'UNIFORM'

    #node Math.005
    math_005 = nser_setex.nodes.new("ShaderNodeMath")
    math_005.label = "32 *gray3"
    math_005.name = "Math.005"
    math_005.operation = 'MULTIPLY_ADD'
    math_005.use_clamp = True
    #Value_001
    math_005.inputs[1].default_value = 10.0
    #Value_002
    math_005.inputs[2].default_value = 9.0

    #node Math.006
    math_006 = nser_setex.nodes.new("ShaderNodeMath")
    math_006.label = "32 gray3"
    math_006.name = "Math.006"
    math_006.operation = 'SUBTRACT'
    math_006.use_clamp = False
    #Value
    math_006.inputs[0].default_value = 1.0

    #node Mix.003
    mix_003 = nser_setex.nodes.new("ShaderNodeMix")
    mix_003.label = "33 fhsv.y"
    mix_003.name = "Mix.003"
    mix_003.blend_type = 'MIX'
    mix_003.clamp_factor = True
    mix_003.clamp_result = False
    mix_003.data_type = 'FLOAT'
    mix_003.factor_mode = 'UNIFORM'

    #node Combine Color.002
    combine_color_002 = nser_setex.nodes.new("ShaderNodeCombineColor")
    combine_color_002.label = "33 fhsv"
    combine_color_002.name = "Combine Color.002"
    combine_color_002.mode = 'HSV'

    #node Mix.004
    mix_004 = nser_setex.nodes.new("ShaderNodeMix")
    mix_004.label = "35 return"
    mix_004.name = "Mix.004"
    mix_004.blend_type = 'MIX'
    mix_004.clamp_factor = True
    mix_004.clamp_result = True
    mix_004.data_type = 'RGBA'
    mix_004.factor_mode = 'UNIFORM'


    #Set locations
    group_input.location = (-580.0, -260.0)
    group_output.location = (2580.0, -400.0)
    separate_xyz_001.location = (-360.0, -420.0)
    combine_color_001.location = (-140.0, -680.0)
    mix.location = (80.0, -480.0)
    vector_math_001.location = (300.0, -220.0)
    separate_color_001.location = (520.0, -220.0)
    combine_xyz.location = (740.0, -220.0)
    vector_math_003.location = (960.0, -220.0)
    math.location = (960.0, -780.0)
    math_002.location = (1180.0, -780.0)
    math_003.location = (1400.0, -400.0)
    math_004.location = (1400.0, -600.0)
    mix_001.location = (1620.0, -400.0)
    separate_xyz_002.location = (1180.0, -220.0)
    mix_002.location = (1620.0, -600.0)
    math_005.location = (960.0, -520.0)
    math_006.location = (1180.0, -520.0)
    mix_003.location = (1840.0, -400.0)
    combine_color_002.location = (2140.0, -400.0)
    mix_004.location = (2360.0, -400.0)

    #Set dimensions
    group_input.width, group_input.height = 140.0, 100.0
    group_output.width, group_output.height = 140.0, 100.0
    separate_xyz_001.width, separate_xyz_001.height = 140.0, 100.0
    combine_color_001.width, combine_color_001.height = 140.0, 100.0
    mix.width, mix.height = 140.0, 100.0
    vector_math_001.width, vector_math_001.height = 140.0, 100.0
    separate_color_001.width, separate_color_001.height = 140.0, 100.0
    combine_xyz.width, combine_xyz.height = 140.0, 100.0
    vector_math_003.width, vector_math_003.height = 140.0, 100.0
    math.width, math.height = 140.0, 100.0
    math_002.width, math_002.height = 140.0, 100.0
    math_003.width, math_003.height = 140.0, 100.0
    math_004.width, math_004.height = 140.0, 100.0
    mix_001.width, mix_001.height = 140.0, 100.0
    separate_xyz_002.width, separate_xyz_002.height = 140.0, 100.0
    mix_002.width, mix_002.height = 140.0, 100.0
    math_005.width, math_005.height = 140.0, 100.0
    math_006.width, math_006.height = 140.0, 100.0
    mix_003.width, mix_003.height = 140.0, 100.0
    combine_color_002.width, combine_color_002.height = 140.0, 100.0
    mix_004.width, mix_004.height = 140.0, 100.0

    #initialize nser_setex links
    #group_input.Colorize -> separate_xyz_001.Vector
    nser_setex.links.new(group_input.outputs[1], separate_xyz_001.inputs[0])
    #separate_xyz_001.X -> combine_color_001.Red
    nser_setex.links.new(separate_xyz_001.outputs[0], combine_color_001.inputs[0])
    #combine_color_001.Color -> mix.B
    nser_setex.links.new(combine_color_001.outputs[0], mix.inputs[7])
    #group_input.Coloring -> mix.Factor
    nser_setex.links.new(group_input.outputs[2], mix.inputs[0])
    #vector_math_001.Vector -> separate_color_001.Color
    nser_setex.links.new(vector_math_001.outputs[0], separate_color_001.inputs[0])
    #separate_color_001.Green -> combine_xyz.Y
    nser_setex.links.new(separate_color_001.outputs[1], combine_xyz.inputs[1])
    #separate_xyz_001.Y -> math.Value
    nser_setex.links.new(separate_xyz_001.outputs[1], math.inputs[0])
    #math.Value -> math_002.Value
    nser_setex.links.new(math.outputs[0], math_002.inputs[1])
    #separate_color_001.Green -> math_003.Value
    nser_setex.links.new(separate_color_001.outputs[1], math_003.inputs[0])
    #separate_xyz_001.Y -> math_003.Value
    nser_setex.links.new(separate_xyz_001.outputs[1], math_003.inputs[1])
    #separate_xyz_001.Z -> math_004.Value
    nser_setex.links.new(separate_xyz_001.outputs[2], math_004.inputs[1])
    #separate_color_001.Blue -> math_004.Value
    nser_setex.links.new(separate_color_001.outputs[2], math_004.inputs[0])
    #math_003.Value -> mix_001.B
    nser_setex.links.new(math_003.outputs[0], mix_001.inputs[3])
    #vector_math_003.Vector -> separate_xyz_002.Vector
    nser_setex.links.new(vector_math_003.outputs[0], separate_xyz_002.inputs[0])
    #separate_xyz_002.Y -> mix_001.A
    nser_setex.links.new(separate_xyz_002.outputs[1], mix_001.inputs[2])
    #math_002.Value -> mix_001.Factor
    nser_setex.links.new(math_002.outputs[0], mix_001.inputs[0])
    #math_002.Value -> mix_002.Factor
    nser_setex.links.new(math_002.outputs[0], mix_002.inputs[0])
    #separate_xyz_001.Y -> math_005.Value
    nser_setex.links.new(separate_xyz_001.outputs[1], math_005.inputs[0])
    #math_005.Value -> math_006.Value
    nser_setex.links.new(math_005.outputs[0], math_006.inputs[1])
    #math_003.Value -> mix_003.B
    nser_setex.links.new(math_003.outputs[0], mix_003.inputs[3])
    #math_006.Value -> mix_003.Factor
    nser_setex.links.new(math_006.outputs[0], mix_003.inputs[0])
    #separate_xyz_002.X -> combine_color_002.Red
    nser_setex.links.new(separate_xyz_002.outputs[0], combine_color_002.inputs[0])
    #mix_003.Result -> combine_color_002.Green
    nser_setex.links.new(mix_003.outputs[0], combine_color_002.inputs[1])
    #mix_001.Result -> mix_003.A
    nser_setex.links.new(mix_001.outputs[0], mix_003.inputs[2])
    #combine_color_002.Color -> mix_004.B
    nser_setex.links.new(combine_color_002.outputs[0], mix_004.inputs[7])
    #group_input.Coloring -> mix_004.Factor
    nser_setex.links.new(group_input.outputs[2], mix_004.inputs[0])
    #mix_002.Result -> combine_color_002.Blue
    nser_setex.links.new(mix_002.outputs[0], combine_color_002.inputs[2])
    #separate_xyz_002.Z -> mix_002.A
    nser_setex.links.new(separate_xyz_002.outputs[2], mix_002.inputs[2])
    #math_004.Value -> mix_002.B
    nser_setex.links.new(math_004.outputs[0], mix_002.inputs[3])
    #group_input.Colorize -> vector_math_003.Vector
    nser_setex.links.new(group_input.outputs[1], vector_math_003.inputs[0])
    #separate_color_001.Blue -> combine_xyz.Z
    nser_setex.links.new(separate_color_001.outputs[2], combine_xyz.inputs[2])
    #mix_004.Result -> group_output.Color
    nser_setex.links.new(mix_004.outputs[2], group_output.inputs[0])
    #combine_xyz.Vector -> vector_math_003.Vector
    nser_setex.links.new(combine_xyz.outputs[0], vector_math_003.inputs[2])
    #mix.Result -> vector_math_001.Vector
    nser_setex.links.new(mix.outputs[2], vector_math_001.inputs[1])
    #group_input.Color -> vector_math_001.Vector
    nser_setex.links.new(group_input.outputs[0], vector_math_001.inputs[0])
    #group_input.Color -> mix_004.A
    nser_setex.links.new(group_input.outputs[0], mix_004.inputs[6])
    return nser_setex

def get_setex() -> bpy.types.ShaderNodeTree:
    setex = bpy.data.node_groups.get(NODE_SETEX)
    if setex is None or setex.type != 'ShaderNodeTree':
        return gen_setex_node()
    return setex

def create_texture(event: TextureEvent, dirname: str) -> bpy.types.Image:
    if event.path is not None:
        path = os.path.join(dirname, event.path.replace('\\', '/'))
        for ext in ('.png', '.PNG', '.dds', '.DDS'):
            if os.path.exists(path + ext):
                path += ext
                break
        try:
            image = bpy.data.images.load(path, check_existing=True)
            image.colorspace_settings.is_data = True # type: ignore[assignment]
            return image
        except:
            pass
    
    return bpy.data.images.new(name=event.name, width=1, height=1)

def create_material(data: Data, event: MaterialEvent) -> bpy.types.Material:
    color_metal_id = event.textures.get(TextureKind.ColorMetal)
    normal_gloss_id = event.textures.get(TextureKind.NormalGloss)
    add_maps_id     = event.textures.get(TextureKind.AddMaps)
    alpha_mask_id   = event.textures.get(TextureKind.AlphaMask)

    color_metal  = data.textures.get(color_metal_id)  if color_metal_id  else None
    add_maps     = data.textures.get(add_maps_id)     if add_maps_id     else None
    normal_gloss = data.textures.get(normal_gloss_id) if normal_gloss_id else None
    alpha_mask   = data.textures.get(alpha_mask_id)   if alpha_mask_id   else None

    material = bpy.data.materials.new(name=f'nSEr {event.id} ({event.name})')
    material.use_nodes = True
    tree = material.node_tree
    assert tree is not None, 'Node tree should not be None here'

    node_output: bpy.types.ShaderNodeOutputMaterial = tree.nodes.get('Material Output') # type: ignore[assignment]
    node_output.location = (800, 0)

    if GLASS_HACK and event.render_mode == RenderMode.Glass:
        node_mix: bpy.types.ShaderNodeMixShader = tree.nodes.new('ShaderNodeMixShader') # type: ignore[assignment]
        node_mix.label = 'Glass Hack'
        node_mix.location = (400, 0)
        tree.links.new(node_mix.outputs['Shader'], node_output.inputs['Surface'])

        node_fresnel: bpy.types.ShaderNodeFresnel = tree.nodes.new('ShaderNodeFresnel') # type: ignore[assignment]
        node_fresnel.location = (400, -200)
        node_fresnel.inputs['IOR'].default_value = 1.45 # type: ignore[assignment]
        tree.links.new(node_fresnel.outputs['Fac'], node_mix.inputs['Fac'])

        node_refraction: bpy.types.ShaderNodeBsdfRefraction = tree.nodes.new('ShaderNodeBsdfRefraction') # type: ignore[assignment]
        node_refraction.location = (400, -400)
        node_refraction.inputs['Roughness'].default_value = 0.0 # type: ignore[assignment]
        node_refraction.inputs['IOR'].default_value = 1.05 # type: ignore[assignment]
        tree.links.new(node_refraction.outputs['BSDF'], node_mix.inputs[1])

        node_glossy: bpy.types.ShaderNodeBsdfGlossy = tree.nodes.new('ShaderNodeBsdfGlossy') # type: ignore[assignment]
        node_glossy.location = (400, -600)
        node_glossy.inputs['Roughness'].default_value = 0.0 # type: ignore[assignment]
        tree.links.new(node_glossy.outputs['BSDF'], node_mix.inputs[2])

        node_reroute_roughness: bpy.types.NodeReroute = tree.nodes.new('NodeReroute') # type: ignore[assignment]
        node_reroute_roughness.socket_idname = 'NodeSocketFloat'
        node_reroute_roughness.location = (380, -150)
        tree.links.new(node_reroute_roughness.outputs[0], node_refraction.inputs['Roughness'])
        tree.links.new(node_reroute_roughness.outputs[0], node_glossy.inputs['Roughness'])

        node_reroute_normal: bpy.types.NodeReroute = tree.nodes.new('NodeReroute') # type: ignore[assignment]
        node_reroute_normal.socket_idname = 'NodeSocketVector'
        node_reroute_normal.location = (380, -250)
        tree.links.new(node_reroute_normal.outputs[0], node_fresnel.inputs['Normal'])
        tree.links.new(node_reroute_normal.outputs[0], node_refraction.inputs['Normal'])
        tree.links.new(node_reroute_normal.outputs[0], node_glossy.inputs['Normal'])

        socket_color = None
        socket_metallic = None
        socket_roughness = node_reroute_roughness.inputs[0]
        socket_normal = node_reroute_normal.inputs[0]
        socket_alpha = None
    else:
        node_bsdf: bpy.types.ShaderNodeBsdfPrincipled = tree.nodes.get('Principled BSDF') # type: ignore[assignment]
        node_bsdf.location = (400, 0)
        tree.links.new(node_bsdf.outputs['BSDF'], node_output.inputs['Surface'])

        if event.render_mode == RenderMode.Glass:
            node_bsdf.inputs['Transmission Weight'].default_value = 1.0 # type: ignore[assigment]
            node_bsdf.inputs['Alpha'].default_value = 0.9 # type: ignore[assigment]
            if normal_gloss is None:
                node_bsdf.inputs['Roughness'].default_value = 0.1 # type: ignore[assignment]

        socket_color = node_bsdf.inputs['Base Color']
        socket_metallic = node_bsdf.inputs['Metallic']
        socket_roughness = node_bsdf.inputs['Roughness']
        socket_normal = node_bsdf.inputs['Normal']
        socket_alpha = node_bsdf.inputs['Alpha']

    node_setex: bpy.types.NodeGroup = tree.nodes.new('ShaderNodeGroup') # type: ignore[assignment]
    node_setex.location = (200, 0)
    node_setex.node_tree = data.setex
    if socket_color is not None:
        tree.links.new(node_setex.outputs['Color'], socket_color)

    node_attr_colorize: bpy.types.ShaderNodeAttribute = tree.nodes.new('ShaderNodeAttribute') # type: ignore[assignment]
    node_attr_colorize.location = (-800, 0)
    node_attr_colorize.attribute_type = 'OBJECT'
    node_attr_colorize.attribute_name = 'colorize'
    tree.links.new(node_attr_colorize.outputs['Vector'], node_setex.inputs['Colorize'])

    if color_metal is not None:
        node_gamma: bpy.types.ShaderNodeGamma = tree.nodes.new('ShaderNodeGamma') # type: ignore[assignment]
        node_gamma.label = 'GammaCorrection'
        node_gamma.location = (0, 0)
        node_gamma.inputs['Gamma'].default_value = 2.2 # type: ignore[assignment]
        tree.links.new(node_gamma.outputs['Color'], node_setex.inputs['Color'])

        node_tex_color: bpy.types.ShaderNodeTexImage = tree.nodes.new('ShaderNodeTexImage') # type: ignore[assignment]
        node_tex_color.label = 'ColorMetal'
        node_tex_color.location = (-400, -600)
        node_tex_color.image = color_metal
        tree.links.new(node_tex_color.outputs['Color'], node_gamma.inputs['Color'])
        if socket_metallic is not None:
            # Use the alpha channel for metallic
            tree.links.new(node_tex_color.outputs['Alpha'], socket_metallic)

    if add_maps is not None:
        node_tex_add: bpy.types.ShaderNodeTexImage = tree.nodes.new('ShaderNodeTexImage') # type: ignore[assignment]
        node_tex_add.label = 'AddMaps'
        node_tex_add.location = (-400, -200)
        node_tex_add.image = add_maps
        tree.links.new(node_tex_add.outputs['Alpha'], node_setex.inputs['Coloring'])

    if normal_gloss is not None:
        node_tex_normal: bpy.types.ShaderNodeTexImage = tree.nodes.new('ShaderNodeTexImage') # type: ignore[assignment]
        node_tex_normal.label = 'NormalGloss'
        node_tex_normal.location = (-400, 200)
        node_tex_normal.image = normal_gloss

        node_normal_map: bpy.types.ShaderNodeNormalMap = tree.nodes.new('ShaderNodeNormalMap') # type: ignore[assignment]
        node_normal_map.label = 'NormalMap'
        node_normal_map.location = (0, -200)
        node_normal_map.inputs['Strength'].default_value = 2.0 # type: ignore[assignment]   
        tree.links.new(node_tex_normal.outputs['Color'], node_normal_map.inputs['Color'])
        tree.links.new(node_normal_map.outputs['Normal'], socket_normal)

        node_gloss_invert: bpy.types.ShaderNodeMath = tree.nodes.new('ShaderNodeMath') # type: ignore[assignment]
        node_gloss_invert.label = 'GlossInvert'
        node_gloss_invert.location = (0, 200)
        node_gloss_invert.operation = 'SUBTRACT'
        node_gloss_invert.inputs[0].default_value = 1.0 # type: ignore[assignment]
        tree.links.new(node_tex_normal.outputs['Alpha'], node_gloss_invert.inputs[1])
        tree.links.new(node_gloss_invert.outputs['Value'], socket_roughness)

    if alpha_mask is not None:
        node_tex_alpha: bpy.types.ShaderNodeTexImage = tree.nodes.new('ShaderNodeTexImage') # type: ignore[assignment]
        node_tex_alpha.label = 'AlphaMask'
        node_tex_alpha.location = (-400, 600)
        node_tex_alpha.image = alpha_mask
        if socket_alpha is not None:
            tree.links.new(node_tex_alpha.outputs['Alpha'], socket_alpha)

    return material

def get_variant_key(event: MaterialEvent) -> VariantKey:
    color_metal_id  = event.textures.get(TextureKind.ColorMetal)
    normal_gloss_id = event.textures.get(TextureKind.NormalGloss)
    add_maps_id     = event.textures.get(TextureKind.AddMaps)
    alpha_mask_id   = event.textures.get(TextureKind.AlphaMask)
    return (event.render_mode, color_metal_id, normal_gloss_id, add_maps_id, alpha_mask_id)

def get_material(data: Data, id: int, overrides: dict[int, int]) -> bpy.types.Material | None:
    if id not in data.materials:
        return None
    event = data.materials[id]
    if event.render_mode == RenderMode.Glass:
        pass  # TODO: Handle glass materials
    if id in overrides:
        event = event.merge(data.materials[overrides[id]])

    key = get_variant_key(event)
    if key in data.variants:
        return data.variants[key]

    material = create_material(data, event)
    data.variants[key] = material
    return material

def create_mesh(event: ModelEvent) -> bpy.types.Mesh:
    mesh = bpy.data.meshes.new(f'nSEr MM {event.id} {event.name}')
    vertices = [swap_yz(v) for v in event.vertices]
    mesh.from_pydata(vertices, [], event.indices)

    material_index = 0
    for mesh_info in event.meshes:
        mesh.materials.append(None)
        for i in range(mesh_info.tri_start, mesh_info.tri_start + mesh_info.tri_count):
            mesh.polygons[i].material_index = material_index
        material_index += 1

    layer = mesh.uv_layers.new()
    for loop in mesh.loops:
        layer.uv[loop.index].vector = event.tex_coords[loop.vertex_index]
    mesh.update()

    return mesh

def create_model(data: Data, event: ModelEvent, overrides: dict[int, int], colorize: Vec3 | None) -> bpy.types.Object:
    obj = bpy.data.objects.new(f'nSEr SM {event.id}', data.meshes[event.id])
    data.collection_entities.objects.link(obj)

    for i, mesh_info in enumerate(event.meshes):
        obj.material_slots[i].link = 'OBJECT'
        obj.material_slots[i].material = get_material(data, mesh_info.mat_id, overrides)
        obj['colorize'] = (colorize or ColorMask_Default) + (1.0,)

    return obj

def set_entity_position(data: Data, obj: bpy.types.Object, event: EntityEvent):
    if event.lmatrix is not None:
        matrix = YZ_MATRIX @ Matrix(event.lmatrix) @ YZ_MATRIX
        matrix.transpose()
        pos, rot, scale = matrix.decompose()

        obj.location = pos
        obj.rotation_quaternion = rot
        obj.scale = scale
    elif event.wmatrix is not None:
        obj.matrix_world = data.view_matrix @ Matrix(event.wmatrix).transposed() @ YZ_MATRIX

def create_entity(data: Data, event: EntityEvent) -> bpy.types.Object | None:
    parent = None
    overrides = dict[int, int]()
    color = ColorMask_Default
    if event.parent is not None:
        parent = data.entities.get(event.parent, None)
        overrides |= data.overrides.get(event.parent, {})
        color = data.colors.get(event.parent, ColorMask_Default)

        if parent is None:
            print(f'Parent {event.parent} not found')
            return None
        
    if event.color:
        color = event.color

    data.overrides[event.id] = overrides
    data.colors[event.id] = color

    if event.model is not None:
        obj = create_model(data, data.models[event.model], overrides, color)
    else:
        obj = bpy.data.objects.new(f'nSEr EE', None)
        data.collection_entities.objects.link(obj)
    
    if parent is not None:
        obj.parent = parent
        obj.name = f'nSEr PM {event.id} {event.name}'
    else:
        obj.name = f'nSEr EM {event.id} {event.name}'

    obj.rotation_mode = 'QUATERNION'
    set_entity_position(data, obj, event)
    
    if event.color is not None:
        obj.color = event.color + (1.0,)

    if data.frame > 0:
        obj.hide_viewport = True
        obj.hide_render = True
        obj.keyframe_insert('hide_render', frame=data.frame-1)
        obj.hide_viewport = False
        obj.hide_render = False
        obj.keyframe_insert('hide_render', frame=data.frame)

    return obj

def update_entity(data: Data, event: EntityEvent) -> None:
    # print(f'Updating entity {event.id}, wmatrix={event.wmatrix is not None}, lmatrix={event.lmatrix is not None}')
    obj = data.entities[event.id]

    if event.remove:
        print('Removing entity', event.id)

    change = event.remove or event.show is not None
    show = event.remove or event.show

    if change:
        print('Change visibility', obj.name, show)
        obj.keyframe_insert('hide_render', frame=data.frame-1)
        obj.hide_viewport = not show
        obj.hide_render = not show
        obj.keyframe_insert('hide_render', frame=data.frame)

    if event.lmatrix is not None or event.wmatrix is not None:
        obj.keyframe_insert('location', frame=data.frame-1)
        obj.keyframe_insert('rotation_quaternion', frame=data.frame-1)
        obj.keyframe_insert('scale', frame=data.frame-1)
        set_entity_position(data, obj, event)
        obj.keyframe_insert('location', frame=data.frame)
        obj.keyframe_insert('rotation_quaternion', frame=data.frame)
        obj.keyframe_insert('scale', frame=data.frame)

def create_block(data: Data, event: BlockEvent) -> bpy.types.Object:
    if event.model is not None:
        overrides = dict((o.src_id, o.dst_id) for o in event.overrides)
        obj = create_model(data, data.models[event.model], overrides, event.color)
        data.overrides[event.id] = overrides
        data.colors[event.id] = event.color
    else:
        obj = bpy.data.objects.new(f'nSEr BE {event.id} {event.position}', None)
        data.collection_entities.objects.link(obj)

    obj.parent = data.entities[event.parent]
    obj.matrix_local = get_matrix(event.translation, event.orientation)
    
    return obj

def update_block(data: Data, event: BlockEvent) -> None:
    obj = data.entities[event.id]

    if event.remove:
        print('Removing block', event.id)

    if event.remove:
        obj.keyframe_insert('hide_render', frame=data.frame-1)
        obj.hide_viewport = True
        obj.hide_render = True
        obj.keyframe_insert('hide_render', frame=data.frame)

LIGHT_YZ_MATRIX = Matrix((
    (1,  0, 0, 0),
    (0, -1, 0, 0),
    (0,  0, 1, 0),
    (0,  0, 0, 1),
))

def create_light(data: Data, event: LightEvent) -> bpy.types.Object:
    # print(f'Create light {event.id}')
    light: Any
    if event.cone:
        inner, outer = event.cone
        light = bpy.data.lights.new(name=f'nSEr Light {event.id}', type='SPOT')
        light.spot_size = outer
        light.spot_blend = 1.0 - inner / outer
    else:
        light = bpy.data.lights.new(name=f'nSEr Light {event.id}', type='POINT')

    (r, g, b) = event.color
    energy = math.sqrt(r*r + g*g + b*b)

    if energy == 0:
        light.color = (r, g, b)
    else:
        light.color = (r / energy, g / energy, b / energy)
    light.energy = energy

    obj = bpy.data.objects.new(name=f'nSEr Light {event.id}', object_data=light)
    data.collection_lights.objects.link(obj)
    obj.rotation_mode = 'QUATERNION'
    obj.matrix_world = data.view_matrix @ Matrix(event.matrix).transposed() @ LIGHT_YZ_MATRIX

    return obj

def update_light(data: Data, event: LightEvent) -> None:
    # print(f'Updating light {event.id}')
    obj = data.lights[event.id]
    obj.keyframe_insert('location', frame=data.frame-1)
    obj.keyframe_insert('rotation_quaternion', frame=data.frame-1)
    obj.keyframe_insert('scale', frame=data.frame-1)
    obj.matrix_world = data.view_matrix @ Matrix(event.matrix).transposed() @ LIGHT_YZ_MATRIX
    obj.keyframe_insert('location', frame=data.frame)
    obj.keyframe_insert('rotation_quaternion', frame=data.frame)
    obj.keyframe_insert('scale', frame=data.frame)

def handle_event(data: Data, event: Event, dirname: str):
    match event:
        case AdvanceEvent():
            # print(f'Advance delta={event.delta}')
            data.frame += round(event.delta * FPS)
            print(f'Frame {data.frame}\u001b[F')
        case TextureEvent():
            # print(f'Texture id={event.id} type={event.ty} name={event.name}')
            texture = create_texture(event, dirname)
            data.textures[event.id] = texture
            
        case MaterialEvent():
            # print(f'Material id={event.id} name={event.name} render={event.render_mode} textures={event.textures}')
            data.materials[event.id] = event

        case ModelEvent():
            # print(f'Model id={event.id} name={event.name} vertices={len(event.vertices)} normals={len(event.normals)} tex_coords={len(event.tex_coords)} indices={len(event.indices)} meshes={len(event.meshes)}')
            mesh = create_mesh(event)
            data.meshes[event.id] = mesh
            data.models[event.id] = event

        case EntityEvent():
            # print(f'Entity id={event.id} entity={event.entity} name={event.name} model={event.model} color={event.color} preview={event.preview} show={event.show} parent={event.parent} wmatrix={event.wmatrix is not None} lmatrix={event.lmatrix is not None}')
            if event.id in data.entities:
                update_entity(data, event)
            else:
                if not event.preview: # TODO: Make configurable
                    entity = create_entity(data, event)
                    if entity is not None:
                        data.entities[event.id] = entity

        case BlockEvent():
            # print(f'Block id={event.id} position={event.position} model={event.model} color={event.color} translation={event.translation} orientation={event.orientation} entity={event.entity}')
            if event.id in data.entities:
                update_block(data, event)
            else:
                data.entities[event.id] = create_block(data, event)

        # case EntityBlocksEvent():
        #     print(f'EntityBlocks id={event.id} scale={event.scale} blocks={len(event.blocks)}')
        #     create_entity_blocks(data, event)

        case LightEvent():
            # print(f'Light id={event.id} color={event.color} cone={event.cone}')
            if event.id in data.lights:
                update_light(data, event)
            else:
                obj = create_light(data, event)
                data.lights[event.id] = obj

def import_semodel(model_path: str, context: bpy.types.Context):
    print('Importing semodel')

    scene = context.scene
    if scene is None:
        raise ValueError('No scene')
    
    dirname = os.path.dirname(os.path.abspath(model_path))

    wm = bpy.context.window_manager

    with open(model_path, 'rb') as f:
        r = BinReader(f)
        assert r.raw(4) == b'nSEr' # magic
        major = r.u16()
        minor = r.u16()
        if major != 1:
            raise ValueError(f'Unsupported version {major}')
        print(f'Importing semodel version {major}.{minor}')
        r.raw(4) # reserved

        header = r.properties()
        anchor = header.get(PropertyTypes.MatrixD, Mat4_Identity)
        print(header)

        data = Data(scene.collection, get_setex(), view_matrix=Matrix(anchor).transposed())

        with ProgressReport(wm) as progress: # type: ignore[context-manager]
            with ProgressReportSubstep(progress, r.length(), 'Importing') as substep: # type: ignore[context-manager]
                last = time.time()
                last_pos = r.tell()

                for event in r.events():
                    if time.time() - last > 1.0:
                        pos = r.tell()
                        substep.step(nbr=pos - last_pos)
                        last_pos = pos
                        # bpy.ops.wm.redraw_timer(type='DRAW_WIN_SWAP', iterations=1)
                        last = time.time()
                    handle_event(data, event, dirname)

            with ProgressReportSubstep(progress, len(data.entities), 'Cleaning up') as substep: # type: ignore[context-manager]
                last = time.time()
                count = 0

                for obj in data.entities.values():
                    if time.time() - last > 1.0:
                        substep.step(nbr=count)
                        count = 0
                        # bpy.ops.wm.redraw_timer(type='DRAW_WIN_SWAP', iterations=1)
                        last = time.time()
                    count += 1
                    if not len(obj.children) and not obj.data:
                        bpy.data.objects.remove(obj, do_unlink=True)

            print()
            print('Done')

class ImportSEModel(bpy.types.Operator, bpx.io_utils.ImportHelper): # type: ignore[override]
    """Import a .semodel file generated by Never-SErender"""
    bl_idname = 'import_scene.semodel'
    bl_label = 'Import semodel'
    bl_options = {'REGISTER', 'UNDO'}

    def invoke(self, context: bpy.types.Context, event: bpy.types.Event): # type: ignore[override]
        print('Importing semodel')
        bpx.io_utils.ImportHelper.invoke_popup(self, context)

        return {'RUNNING_MODAL'}

    def execute(self, context: bpy.types.Context): # type: ignore
        self.filepath: str

        print(self.filepath)
        import_semodel(self.filepath, context)

        return {'FINISHED'}

def menu_func(self, context):
    self.layout.operator(ImportSEModel.bl_idname, text='Never-SErender (.semodel)')

def register():
    print('Registering never-serender')
    bpy.utils.register_class(ImportSEModel)
    bpy.types.TOPBAR_MT_file_import.append(menu_func)
    gen_setex_node()

def unregister():
    print('Unregistering never-serender')
    bpy.utils.unregister_class(ImportSEModel)
    bpy.types.TOPBAR_MT_file_import.remove(menu_func)


if __name__ == "__main__":
    register()
