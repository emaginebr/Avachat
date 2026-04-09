using Avachat.Domain.Enums;
using Avachat.Domain.Models;
using Avachat.Infra.Interfaces.AppServices;
using Avachat.Infra.Interfaces.Repository;
using Microsoft.Extensions.Logging;

namespace Avachat.Application.Services;

public class IngestionService
{
    private readonly IKnowledgeFileRepository<KnowledgeFile> _fileRepository;
    private readonly IElasticsearchService _esService;
    private readonly IOpenAIService _openAIService;
    private readonly ILogger<IngestionService> _logger;

    public IngestionService(
        IKnowledgeFileRepository<KnowledgeFile> fileRepository,
        IElasticsearchService esService,
        IOpenAIService openAIService,
        ILogger<IngestionService> logger)
    {
        _fileRepository = fileRepository;
        _esService = esService;
        _openAIService = openAIService;
        _logger = logger;
    }

    public async Task ProcessFileAsync(long fileId)
    {
        var file = await _fileRepository.GetByIdAsync(fileId);
        if (file == null)
        {
            _logger.LogWarning("[Ingestion] Arquivo ID={FileId} nao encontrado", fileId);
            return;
        }

        _logger.LogInformation(
            "[Ingestion] Iniciando processamento do arquivo ID={FileId}, Nome={FileName}, AgentId={AgentId}, Tamanho={FileSize} bytes",
            fileId, file.FileName, file.AgentId, file.FileSize);

        try
        {
            file.ProcessingStatus = ProcessingStatus.Processing;
            file.ErrorMessage = null;
            await _fileRepository.UpdateAsync(file);

            // Delete old chunks if reprocessing
            _logger.LogInformation("[Ingestion] Removendo chunks antigos do arquivo ID={FileId} no Elasticsearch", fileId);
            await _esService.DeleteChunksByFileIdAsync(fileId);

            // Chunk the content
            var chunks = ChunkText(file.FileContent, 2000, 200);
            _logger.LogInformation(
                "[Ingestion] Arquivo ID={FileId} dividido em {ChunkCount} chunk(s) (tamanho=2000, overlap=200)",
                fileId, chunks.Count);

            for (int i = 0; i < chunks.Count; i++)
            {
                var preview = chunks[i].Length > 150 ? chunks[i][..150] + "..." : chunks[i];
                _logger.LogInformation(
                    "[Ingestion] Chunk [{Index}/{Total}] ({Length} chars): {Preview}",
                    i + 1, chunks.Count, chunks[i].Length, preview);
            }

            // Generate embeddings and index
            var chunkDataList = new List<ChunkData>();
            for (int i = 0; i < chunks.Count; i++)
            {
                _logger.LogInformation(
                    "[Ingestion] Gerando embedding para chunk [{Index}/{Total}]...",
                    i + 1, chunks.Count);

                var embedding = await _openAIService.GenerateEmbeddingAsync(chunks[i]);

                _logger.LogInformation(
                    "[Ingestion] Embedding gerado para chunk [{Index}/{Total}] - {Dimensions} dimensoes",
                    i + 1, chunks.Count, embedding.Length);

                chunkDataList.Add(new ChunkData
                {
                    Content = chunks[i],
                    Embedding = embedding,
                    ChunkIndex = i
                });
            }

            _logger.LogInformation(
                "[Ingestion] Indexando {ChunkCount} chunk(s) no Elasticsearch para AgentId={AgentId}, FileId={FileId}",
                chunkDataList.Count, file.AgentId, fileId);

            await _esService.IndexChunksAsync(file.AgentId!.Value, fileId, chunkDataList);

            file.ProcessingStatus = ProcessingStatus.Ready;
            await _fileRepository.UpdateAsync(file);

            _logger.LogInformation(
                "[Ingestion] Processamento concluido com sucesso para arquivo ID={FileId}, Nome={FileName} - {ChunkCount} chunk(s) indexados",
                fileId, file.FileName, chunkDataList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Ingestion] Erro ao processar arquivo ID={FileId}, Nome={FileName}: {Error}",
                fileId, file.FileName, ex.Message);

            file.ProcessingStatus = ProcessingStatus.Error;
            file.ErrorMessage = ex.Message;
            await _fileRepository.UpdateAsync(file);
        }
    }

    public static List<string> ChunkText(string text, int chunkSize = 2000, int overlap = 200)
    {
        var chunks = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return chunks;

        // Split on double newlines first for natural boundaries
        var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

        var currentChunk = string.Empty;

        foreach (var paragraph in paragraphs)
        {
            if (currentChunk.Length + paragraph.Length + 2 > chunkSize && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.Trim());
                // Keep overlap from end of previous chunk
                var overlapStart = Math.Max(0, currentChunk.Length - overlap);
                currentChunk = currentChunk[overlapStart..] + "\n\n" + paragraph;
            }
            else
            {
                currentChunk += (currentChunk.Length > 0 ? "\n\n" : "") + paragraph;
            }
        }

        if (!string.IsNullOrWhiteSpace(currentChunk))
        {
            chunks.Add(currentChunk.Trim());
        }

        return chunks;
    }
}
