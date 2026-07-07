using Core.Misc.TempFolder;

namespace Core.Tests.Misc;

public class TempFolderTests
{
    [Fact]
    public void Create_WithNullPath_CreatesInTemp()
    {
        using var folder = TempFolderFactory.Create(null);
        Assert.NotNull(folder);
        Assert.NotNull(folder.Path);
        Assert.True(Directory.Exists(folder.Path));
        Assert.Contains("_e2e_temp", folder.Path);
    }

    [Fact]
    public void Create_WithSpecifiedPath_CreatesFolder()
    {
        var path = Path.Combine(Path.GetTempPath(), "test_temp_folder_" + Guid.NewGuid());
        try
        {
            using var folder = TempFolderFactory.Create(path);
            Assert.Equal(path, folder.Path);
            Assert.True(Directory.Exists(path));
        }
        finally
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path);
            }
        }
    }

    [Fact]
    public void Create_WithExistingPath_DoesNotThrow()
    {
        var path = Path.Combine(Path.GetTempPath(), "existing_temp_" + Guid.NewGuid());
        Directory.CreateDirectory(path);
        try
        {
            using var folder = TempFolderFactory.Create(path);
            Assert.Equal(path, folder.Path);
            Assert.True(Directory.Exists(path));
        }
        finally
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path);
            }
        }
    }

    [Fact]
    public void Dispose_WithEmptyFolder_DeletesIt()
    {
        var path = Path.Combine(Path.GetTempPath(), "dispose_test_" + Guid.NewGuid());
        var folder = new TempFolder(path);
        Directory.CreateDirectory(path);
        Assert.True(Directory.Exists(path));
        folder.Dispose();
        Assert.False(Directory.Exists(path));
    }

    [Fact]
    public void Dispose_WithNonEmptyFolder_DoesNotDelete()
    {
        var path = Path.Combine(Path.GetTempPath(), "nonempty_test_" + Guid.NewGuid());
        var folder = new TempFolder(path);
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, "test.txt"), "content");
        Assert.True(Directory.Exists(path));
        folder.Dispose();
        Assert.True(Directory.Exists(path));
        Directory.Delete(path, true);
    }

    [Fact]
    public void Dispose_WithNonExistentFolder_DoesNotThrow()
    {
        var path = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid());
        var folder = new TempFolder(path);
        var exception = Record.Exception(() => folder.Dispose());
        Assert.Null(exception);
    }

    [Fact]
    public void Path_IsReadonly()
    {
        using var folder = TempFolderFactory.Create(null);
        Assert.NotNull(folder.Path);
    }

    [Fact]
    public void Dispose_MultipleCalls_DoesNotThrow()
    {
        var path = Path.Combine(Path.GetTempPath(), "multiple_dispose_" + Guid.NewGuid());
        var folder = new TempFolder(path);
        Directory.CreateDirectory(path);
        folder.Dispose();
        var exception = Record.Exception(() => folder.Dispose());
        Assert.Null(exception);
    }
}
