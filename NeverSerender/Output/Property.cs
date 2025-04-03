namespace NeverSerender.Output
{
    public enum Property
    {
        EndHeader = 0x0000,
        Id = 0x0108,
        Name = 0x02FF,
        Author = 0x03FF,
        Path = 0x04FF,
        Matrix = 0x0540,
        MatrixD = 0x0580,
        TextureType = 0x0601,
        Vertices = 0x07FF,
        Normals = 0x08FF,
        TexCoords = 0x09FF,
        Indices = 0x0AFF,
        Meshes = 0x0BFF,
        MaterialOverrides = 0x0CFF,
        Model = 0x0D04,
        Color = 0x0E0C,
        Delta = 0x0F04,
        Cone = 0x1008,
        Scale = 0x1104,
        Remove = 0x1200,
        Preview = 0x1301,
        Parent = 0x1408,
        Show = 0x1501,
    }
}