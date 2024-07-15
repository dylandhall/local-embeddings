namespace LocalEmbeddings.Models;

public record StoreDocumentMarqoRequest(List<Document> documents, List<string> tensorFields);