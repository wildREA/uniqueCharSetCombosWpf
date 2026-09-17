using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Numerics;

namespace uniqueCharSetCombosWpf;

public sealed class ComboGenerationResult
{
    public ComboGenerationResult(
        IReadOnlyList<string> combinations,
        TimeSpan buildTime)
    {
        Combinations = combinations;
        BuildTime = buildTime;
    }

    public IReadOnlyList<string> Combinations { get; }

    public int Count => Combinations.Count;

    public TimeSpan BuildTime { get; }
}

public static class CharComboService
{
    public static ComboGenerationResult run_process(
        string target,
        IProgress<double>? progress = null)
    {
        Stopwatch total_sw = Stopwatch.StartNew();
        progress?.Report(0);

        string[] all_words = File.ReadAllLines(target);
        string[] valid_words = purge_invalid_words(all_words);
        progress?.Report(5);

        HashSet<string> combinations = [];
        find_combos(valid_words, combinations, progress);

        total_sw.Stop();
        progress?.Report(100);

        return new ComboGenerationResult([.. combinations], total_sw.Elapsed);
    }

    private static void find_combos(
        string[] valid_words,
        HashSet<string> combinations,
        IProgress<double>? progress)
    {
        int words_per_combo = 5;
        Dictionary<char, int> char_indices = [];
        ulong[] word_masks = new ulong[valid_words.Length];

        for (int word_i = 0; word_i < valid_words.Length; word_i++)
        {
            ulong mask = 0;
            foreach (char c in valid_words[word_i])
            {
                if (!char_indices.TryGetValue(c, out int char_i))
                {
                    char_i = char_indices.Count;
                    if (char_i == 64)
                    {
                        throw new InvalidOperationException(
                            "The input contains more than 64 distinct characters.");
                    }

                    char_indices.Add(c, char_i);
                }

                mask |= 1UL << char_i;
            }

            word_masks[word_i] = mask;
        }

        progress?.Report(10);

        int[] selected_indices = new int[words_per_combo];
        int[] initial_candidates = new int[valid_words.Length];
        for (int i = 0; i < initial_candidates.Length; i++)
        {
            initial_candidates[i] = i;
        }

        void build_combos(
            int[] candidates,
            int candidate_count,
            int selected_count)
        {
            if (selected_count == words_per_combo)
            {
                combinations.Add(string.Join(' ', selected_indices.Select(index => valid_words[index])));
                return;
            }

            int words_needed = words_per_combo - selected_count;
            if (candidate_count < words_needed)
            {
                return;
            }

            Span<int> character_counts = stackalloc int[64];
            for (int candidate_i = 0; candidate_i < candidate_count; candidate_i++)
            {
                ulong remaining_mask = word_masks[candidates[candidate_i]];
                while (remaining_mask != 0)
                {
                    int character_i = BitOperations.TrailingZeroCount(remaining_mask);
                    character_counts[character_i]++;
                    remaining_mask &= remaining_mask - 1;
                }
            }

            int pivot_character = -1;
            int pivot_count = int.MaxValue;
            for (int character_i = 0; character_i < char_indices.Count; character_i++)
            {
                int count = character_counts[character_i];
                if (count > 0 && count < pivot_count)
                {
                    pivot_character = character_i;
                    pivot_count = count;
                }
            }

            if (pivot_character < 0)
            {
                return;
            }

            ulong pivot_bit = 1UL << pivot_character;

            // Include the pivot character in exactly one selected word.
            for (int candidate_position = 0;
                candidate_position < candidate_count;
                candidate_position++)
            {
                int word_i = candidates[candidate_position];
                ulong word_mask = word_masks[word_i];
                if ((word_mask & pivot_bit) == 0)
                {
                    continue;
                }

                selected_indices[selected_count] = word_i;
                int[] next_candidates = ArrayPool<int>.Shared.Rent(Math.Max(candidate_count - 1, 1));
                int next_count = 0;

                for (int next_position = 0;
                    next_position < candidate_count;
                    next_position++)
                {
                    int next_word_i = candidates[next_position];
                    if (next_word_i != word_i &&
                        (word_mask & word_masks[next_word_i]) == 0)
                    {
                        next_candidates[next_count++] = next_word_i;
                    }
                }

                build_combos(
                    next_candidates,
                    next_count,
                    selected_count + 1);

                ArrayPool<int>.Shared.Return(next_candidates);
            }

            // Exclude the pivot character from the remaining solution when needed.
            if (pivot_count < candidate_count)
            {
                int[] excluded_candidates = ArrayPool<int>.Shared.Rent(Math.Max(candidate_count, 1));
                int excluded_count = 0;
                for (int candidate_i = 0; candidate_i < candidate_count; candidate_i++)
                {
                    int word_i = candidates[candidate_i];
                    if ((word_masks[word_i] & pivot_bit) == 0)
                    {
                        excluded_candidates[excluded_count++] = word_i;
                    }
                }

                build_combos(excluded_candidates, excluded_count, selected_count);
                ArrayPool<int>.Shared.Return(excluded_candidates);
            }

        }

        build_combos(initial_candidates, initial_candidates.Length, 0);
    }

    private static string[] purge_invalid_words(
        string[] all_words)
    {
        int allowed_length = 5;
        string[] valid_words = [.. all_words
            .Select(word => word.Trim())
            .Where(word =>
                word.Length == allowed_length &&
                !has_duplicate_char(word))];
        return valid_words;
    }

    private static bool has_duplicate_char(
        string valid_target)
    {
        var seen = new HashSet<char>();

        foreach (char c in valid_target)
        {
            if (!seen.Add(c)) { return true; }
        }

        return false;
    }
}
