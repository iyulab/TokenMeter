namespace TokenMeter;

/// <summary>
/// How the model exposes its reasoning/thinking content in the API response.
/// </summary>
public enum ThinkingFormat
{
    /// <summary>No thinking content exposed in the response.</summary>
    None = 0,

    /// <summary>
    /// Thinking is delivered as a separate content block with a distinct type
    /// (e.g., Anthropic <c>thinking</c> content block alongside <c>text</c> blocks).
    /// </summary>
    Block,

    /// <summary>
    /// Thinking is embedded as inline tags within the text output
    /// (e.g., DeepSeek R1 <c>&lt;think&gt;...&lt;/think&gt;</c>).
    /// Use <c>ModelInfo.ThinkingTagPattern</c> for the exact pattern.
    /// </summary>
    InlineTag,

    /// <summary>
    /// Thinking is delivered in a separate top-level response field
    /// (e.g., OpenAI o-series <c>reasoning_content</c> field).
    /// Use <c>ModelInfo.ThinkingFieldName</c> for the field name.
    /// </summary>
    SeparateField,
}
