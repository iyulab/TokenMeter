namespace TokenMeter;

/// <summary>
/// Primary output type of the model.
/// </summary>
public enum ModelType
{
    /// <summary>Chat/instruction model that generates text responses.</summary>
    Chat = 0,

    /// <summary>Produces vector embeddings from text input.</summary>
    Embedding,

    /// <summary>Scores relevance of document pairs (cross-encoder).</summary>
    Reranker,

    /// <summary>Generates images from text prompts.</summary>
    ImageGeneration,

    /// <summary>Converts text to speech audio.</summary>
    TextToSpeech,

    /// <summary>Converts speech audio to text.</summary>
    SpeechToText,
}
