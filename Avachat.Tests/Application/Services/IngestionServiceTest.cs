using Xunit;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Avachat.Application.Services;
using Avachat.Domain.Enums;
using Avachat.Domain.Models;
using Avachat.Infra.Interfaces.AppServices;
using Avachat.Infra.Interfaces.Repository;

namespace Avachat.Tests.Application.Services;

public class IngestionServiceTest
{
    private readonly Mock<IKnowledgeFileRepository<KnowledgeFile>> _fileRepoMock;
    private readonly Mock<IElasticsearchService> _esServiceMock;
    private readonly Mock<IOpenAIService> _openAIServiceMock;
    private readonly IngestionService _sut;

    public IngestionServiceTest()
    {
        _fileRepoMock = new Mock<IKnowledgeFileRepository<KnowledgeFile>>();
        _esServiceMock = new Mock<IElasticsearchService>();
        _openAIServiceMock = new Mock<IOpenAIService>();
        _sut = new IngestionService(_fileRepoMock.Object, _esServiceMock.Object, _openAIServiceMock.Object, NullLogger<IngestionService>.Instance);
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldDoNothing_WhenFileNotFound()
    {
        // Arrange
        _fileRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((KnowledgeFile?)null);

        // Act
        await _sut.ProcessFileAsync(999);

        // Assert
        _esServiceMock.Verify(s => s.DeleteChunksByFileIdAsync(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldSetStatusReady_WhenSuccessful()
    {
        // Arrange
        var file = new KnowledgeFile
        {
            KnowledgeFileId = 1,
            AgentId = 10,
            FileContent = "Simple content",
            ProcessingStatus = ProcessingStatus.Processing
        };
        _fileRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(file);
        _fileRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KnowledgeFile>())).ReturnsAsync((KnowledgeFile f) => f);
        _openAIServiceMock.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<string>())).ReturnsAsync(new float[] { 0.1f });

        // Act
        await _sut.ProcessFileAsync(1);

        // Assert
        Assert.Equal(ProcessingStatus.Ready, file.ProcessingStatus);
        _esServiceMock.Verify(s => s.DeleteChunksByFileIdAsync(1), Times.Once);
        _esServiceMock.Verify(s => s.IndexChunksAsync(10, 1, It.IsAny<List<ChunkData>>()), Times.Once);
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldSetStatusError_WhenExceptionOccurs()
    {
        // Arrange
        var file = new KnowledgeFile
        {
            KnowledgeFileId = 1,
            AgentId = 10,
            FileContent = "Content",
            ProcessingStatus = ProcessingStatus.Processing
        };
        _fileRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(file);
        _fileRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KnowledgeFile>())).ReturnsAsync((KnowledgeFile f) => f);
        _esServiceMock.Setup(s => s.DeleteChunksByFileIdAsync(1)).ThrowsAsync(new Exception("ES error"));

        // Act
        await _sut.ProcessFileAsync(1);

        // Assert
        Assert.Equal(ProcessingStatus.Error, file.ProcessingStatus);
        Assert.Equal("ES error", file.ErrorMessage);
    }

    // --- Upload: chunks are sent to Elasticsearch ---

    [Fact]
    public async Task ProcessFileAsync_ShouldSendCorrectNumberOfChunks_ToElasticsearch()
    {
        var content = "Paragrafo 1 com conteudo.\n\nParagrafo 2 com conteudo.\n\nParagrafo 3 com conteudo.";
        var file = new KnowledgeFile
        {
            KnowledgeFileId = 5, AgentId = 10, FileContent = content,
            ProcessingStatus = ProcessingStatus.Processing
        };
        _fileRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(file);
        _fileRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KnowledgeFile>())).ReturnsAsync((KnowledgeFile f) => f);
        _openAIServiceMock.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<string>())).ReturnsAsync(new float[] { 0.1f, 0.2f });

        await _sut.ProcessFileAsync(5);

        var expectedChunks = IngestionService.ChunkText(content, 2000, 200);

        _esServiceMock.Verify(s => s.IndexChunksAsync(
            10, 5,
            It.Is<List<ChunkData>>(list => list.Count == expectedChunks.Count)),
            Times.Once);
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldSendChunksWithCorrectContent()
    {
        var content = "Primeiro paragrafo do documento.\n\nSegundo paragrafo do documento.";
        var file = new KnowledgeFile
        {
            KnowledgeFileId = 1, AgentId = 10, FileContent = content,
            ProcessingStatus = ProcessingStatus.Processing
        };
        _fileRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(file);
        _fileRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KnowledgeFile>())).ReturnsAsync((KnowledgeFile f) => f);
        _openAIServiceMock.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<string>())).ReturnsAsync(new float[] { 0.5f });

        List<ChunkData>? capturedChunks = null;
        _esServiceMock.Setup(s => s.IndexChunksAsync(10, 1, It.IsAny<List<ChunkData>>()))
            .Callback<long, long, List<ChunkData>>((_, _, chunks) => capturedChunks = chunks);

        await _sut.ProcessFileAsync(1);

        Assert.NotNull(capturedChunks);
        Assert.Contains(capturedChunks, c => c.Content.Contains("Primeiro paragrafo"));
        Assert.Contains(capturedChunks, c => c.Content.Contains("Segundo paragrafo"));
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldSendChunksWithEmbeddings()
    {
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };
        var file = new KnowledgeFile
        {
            KnowledgeFileId = 1, AgentId = 10, FileContent = "Conteudo simples",
            ProcessingStatus = ProcessingStatus.Processing
        };
        _fileRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(file);
        _fileRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KnowledgeFile>())).ReturnsAsync((KnowledgeFile f) => f);
        _openAIServiceMock.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<string>())).ReturnsAsync(embedding);

        List<ChunkData>? capturedChunks = null;
        _esServiceMock.Setup(s => s.IndexChunksAsync(10, 1, It.IsAny<List<ChunkData>>()))
            .Callback<long, long, List<ChunkData>>((_, _, chunks) => capturedChunks = chunks);

        await _sut.ProcessFileAsync(1);

        Assert.NotNull(capturedChunks);
        Assert.All(capturedChunks, c => Assert.Equal(embedding, c.Embedding));
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldSendChunksWithCorrectIndexes()
    {
        var content = string.Join("\n\n", Enumerable.Range(1, 5).Select(i => $"Paragrafo {i} com conteudo suficiente."));
        var file = new KnowledgeFile
        {
            KnowledgeFileId = 1, AgentId = 10, FileContent = content,
            ProcessingStatus = ProcessingStatus.Processing
        };
        _fileRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(file);
        _fileRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KnowledgeFile>())).ReturnsAsync((KnowledgeFile f) => f);
        _openAIServiceMock.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<string>())).ReturnsAsync(new float[] { 0.1f });

        List<ChunkData>? capturedChunks = null;
        _esServiceMock.Setup(s => s.IndexChunksAsync(10, 1, It.IsAny<List<ChunkData>>()))
            .Callback<long, long, List<ChunkData>>((_, _, chunks) => capturedChunks = chunks);

        await _sut.ProcessFileAsync(1);

        Assert.NotNull(capturedChunks);
        for (int i = 0; i < capturedChunks.Count; i++)
        {
            Assert.Equal(i, capturedChunks[i].ChunkIndex);
        }
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldGenerateEmbeddingForEachChunk()
    {
        var content = "Paragrafo A.\n\nParagrafo B.\n\nParagrafo C.";
        var expectedChunks = IngestionService.ChunkText(content, 2000, 200);
        var file = new KnowledgeFile
        {
            KnowledgeFileId = 1, AgentId = 10, FileContent = content,
            ProcessingStatus = ProcessingStatus.Processing
        };
        _fileRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(file);
        _fileRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KnowledgeFile>())).ReturnsAsync((KnowledgeFile f) => f);
        _openAIServiceMock.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<string>())).ReturnsAsync(new float[] { 0.1f });

        await _sut.ProcessFileAsync(1);

        _openAIServiceMock.Verify(s => s.GenerateEmbeddingAsync(It.IsAny<string>()), Times.Exactly(expectedChunks.Count));
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldDeleteOldChunks_BeforeIndexingNew()
    {
        var callOrder = new List<string>();
        var file = new KnowledgeFile
        {
            KnowledgeFileId = 1, AgentId = 10, FileContent = "Conteudo",
            ProcessingStatus = ProcessingStatus.Processing
        };
        _fileRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(file);
        _fileRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KnowledgeFile>())).ReturnsAsync((KnowledgeFile f) => f);
        _openAIServiceMock.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<string>())).ReturnsAsync(new float[] { 0.1f });

        _esServiceMock.Setup(s => s.DeleteChunksByFileIdAsync(1))
            .Callback(() => callOrder.Add("delete"));
        _esServiceMock.Setup(s => s.IndexChunksAsync(10, 1, It.IsAny<List<ChunkData>>()))
            .Callback(() => callOrder.Add("index"));

        await _sut.ProcessFileAsync(1);

        Assert.Equal(2, callOrder.Count);
        Assert.Equal("delete", callOrder[0]);
        Assert.Equal("index", callOrder[1]);
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldSendCorrectAgentIdAndFileId()
    {
        var file = new KnowledgeFile
        {
            KnowledgeFileId = 42, AgentId = 99, FileContent = "Conteudo",
            ProcessingStatus = ProcessingStatus.Processing
        };
        _fileRepoMock.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(file);
        _fileRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KnowledgeFile>())).ReturnsAsync((KnowledgeFile f) => f);
        _openAIServiceMock.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<string>())).ReturnsAsync(new float[] { 0.1f });

        await _sut.ProcessFileAsync(42);

        _esServiceMock.Verify(s => s.DeleteChunksByFileIdAsync(42), Times.Once);
        _esServiceMock.Verify(s => s.IndexChunksAsync(99, 42, It.IsAny<List<ChunkData>>()), Times.Once);
    }

    // --- Delete: chunks are removed from Elasticsearch ---

    [Fact]
    public async Task ProcessFileAsync_ShouldDeleteChunksFromElasticsearch_WhenReprocessing()
    {
        var file = new KnowledgeFile
        {
            KnowledgeFileId = 7, AgentId = 10, FileContent = "Reprocessar",
            ProcessingStatus = ProcessingStatus.Ready
        };
        _fileRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(file);
        _fileRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KnowledgeFile>())).ReturnsAsync((KnowledgeFile f) => f);
        _openAIServiceMock.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<string>())).ReturnsAsync(new float[] { 0.1f });

        await _sut.ProcessFileAsync(7);

        _esServiceMock.Verify(s => s.DeleteChunksByFileIdAsync(7), Times.Once);
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldNotIndexChunks_WhenDeleteFails()
    {
        var file = new KnowledgeFile
        {
            KnowledgeFileId = 1, AgentId = 10, FileContent = "Conteudo",
            ProcessingStatus = ProcessingStatus.Processing
        };
        _fileRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(file);
        _fileRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KnowledgeFile>())).ReturnsAsync((KnowledgeFile f) => f);
        _esServiceMock.Setup(s => s.DeleteChunksByFileIdAsync(1)).ThrowsAsync(new Exception("ES delete error"));

        await _sut.ProcessFileAsync(1);

        _esServiceMock.Verify(s => s.IndexChunksAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<List<ChunkData>>()), Times.Never);
        Assert.Equal(ProcessingStatus.Error, file.ProcessingStatus);
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldSetError_WhenIndexingFails()
    {
        var file = new KnowledgeFile
        {
            KnowledgeFileId = 1, AgentId = 10, FileContent = "Conteudo",
            ProcessingStatus = ProcessingStatus.Processing
        };
        _fileRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(file);
        _fileRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KnowledgeFile>())).ReturnsAsync((KnowledgeFile f) => f);
        _openAIServiceMock.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<string>())).ReturnsAsync(new float[] { 0.1f });
        _esServiceMock.Setup(s => s.IndexChunksAsync(10, 1, It.IsAny<List<ChunkData>>()))
            .ThrowsAsync(new Exception("Index failed"));

        await _sut.ProcessFileAsync(1);

        Assert.Equal(ProcessingStatus.Error, file.ProcessingStatus);
        Assert.Equal("Index failed", file.ErrorMessage);
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldHandleLargeDocument_WithMultipleChunks()
    {
        var content = string.Join("\n\n", Enumerable.Range(1, 50).Select(i =>
            $"Paragrafo {i}: " + new string('x', 100)));
        var expectedChunks = IngestionService.ChunkText(content, 2000, 200);
        var file = new KnowledgeFile
        {
            KnowledgeFileId = 1, AgentId = 10, FileContent = content,
            ProcessingStatus = ProcessingStatus.Processing
        };
        _fileRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(file);
        _fileRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KnowledgeFile>())).ReturnsAsync((KnowledgeFile f) => f);
        _openAIServiceMock.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<string>())).ReturnsAsync(new float[] { 0.1f });

        await _sut.ProcessFileAsync(1);

        _esServiceMock.Verify(s => s.IndexChunksAsync(10, 1,
            It.Is<List<ChunkData>>(list => list.Count == expectedChunks.Count)), Times.Once);
        _openAIServiceMock.Verify(s => s.GenerateEmbeddingAsync(It.IsAny<string>()), Times.Exactly(expectedChunks.Count));
        Assert.Equal(ProcessingStatus.Ready, file.ProcessingStatus);
    }

    // --- ChunkText static method tests ---

    [Fact]
    public void ChunkText_ShouldReturnEmpty_WhenTextIsEmpty()
    {
        var result = IngestionService.ChunkText("");
        Assert.Empty(result);
    }

    [Fact]
    public void ChunkText_ShouldReturnEmpty_WhenTextIsWhitespace()
    {
        var result = IngestionService.ChunkText("   ");
        Assert.Empty(result);
    }

    [Fact]
    public void ChunkText_ShouldReturnSingleChunk_WhenTextIsSmall()
    {
        var result = IngestionService.ChunkText("Hello world");
        Assert.Single(result);
        Assert.Equal("Hello world", result[0]);
    }

    [Fact]
    public void ChunkText_ShouldSplitOnDoubleNewlines()
    {
        var text = string.Join("\n\n", Enumerable.Range(1, 50).Select(i => $"Paragraph {i} with some content to fill space."));

        var result = IngestionService.ChunkText(text, 200, 50);

        Assert.True(result.Count > 1);
    }

    [Fact]
    public void ChunkText_ShouldRespectChunkSize()
    {
        var text = string.Join("\n\n", Enumerable.Range(1, 100).Select(i => $"Paragraph {i} with content."));

        var result = IngestionService.ChunkText(text, 100, 20);

        foreach (var chunk in result)
        {
            Assert.True(chunk.Length <= 200, $"Chunk too large: {chunk.Length} chars");
        }
    }
}
