namespace AmasiaLabs.Toolkit.FlowflakeId.Extensions.Codecs;

// Shared reverse-lookup table for the character-indexed codecs in this namespace. Each codec
// maps an input character to its position in a fixed alphabet; centralising the sbyte[256] table
// and the (c < 256 ? map[(byte)c] : -1) lookup keeps the five codecs that need it from each
// carrying their own identical copy.
internal static class CodecCharMap
{
    // Builds a 256-entry map from character to alphabet index; unmapped bytes are -1. When
    // <paramref name="foldCase"/> is true each ASCII letter is mapped under both cases (used by
    // the case-insensitive codecs). Callers that also alias extra characters (e.g. Crockford's
    // I/L/O) apply those to the returned array.
    public static sbyte[] Build(ReadOnlySpan<char> alphabet, bool foldCase = false)
    {
        var map = new sbyte[256];
        Array.Fill(map, (sbyte)-1);
        for (var i = 0; i < alphabet.Length; i++)
        {
            var c = alphabet[i];
            map[(byte)c] = (sbyte)i;
            if (!foldCase) continue;
            map[(byte)char.ToLowerInvariant(c)] = (sbyte)i;
            map[(byte)char.ToUpperInvariant(c)] = (sbyte)i;
        }

        return map;
    }

    // Index of a character in the map, or -1 for any non-ASCII / unmapped character.
    public static int IndexOf(sbyte[] map, char c) => c < 256 ? map[(byte)c] : -1;
}
