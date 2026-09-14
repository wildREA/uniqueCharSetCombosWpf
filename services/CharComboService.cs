using System.IO;
using System.Buffers;
using System.Diagnostics;

namespace uniqueCharSetCombosWpf;

public static class Program
{
    public static (long Count, TimeSpan, BuildTime) run_process(string target)
    {
        string[] all_words = File.ReadAllLines(target);
        string[] valid_words = purge_invalid_words(all_words);
        
        long combo_count = find_combos(
            valid_words,
            out TimeSpan elapsed);

        return elapsed;
    }

    static long find_combos(
        string[] valid_words,
        out TimeSpan elapsed)
    {
        Stopwatch sw = Stopwatch.StartNew();

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

        int[] selected_indices = new int[words_per_combo];
        int[] initial_candidates = new int[valid_words.Length];
        for (int i = 0; i < initial_candidates.Length; i++)
        {
            initial_candidates[i] = i;
        }

        long combo_count = 0;

        void build_combos(
            int[] candidates,
            int candidate_count,
            int selected_count)
        {
            if (selected_count == words_per_combo)
            {
                combo_count++;
                return;
            }

            int words_needed = words_per_combo - selected_count;
            if (candidate_count < words_needed)
            {
                return;
            }

            int pivot_character = -1;
            int pivot_count = int.MaxValue;

            for (int character_i = 0; character_i < char_indices.Count; character_i++)
            {
                ulong character_bit = 1UL << character_i;
                int count = 0;
                for (int candidate_i = 0; candidate_i < candidate_count; candidate_i++)
                {
                    if ((word_masks[candidates[candidate_i]] & character_bit) != 0)
                    {
                        count++;
                    }
                }

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

            // Exclude the pivot character from the remaining solution.
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

        build_combos(initial_candidates, initial_candidates.Length, 0);

        sw.Stop();
        elapsed = sw.Elapsed;

        return combo_count;
    }

    static string[] purge_invalid_words(
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

    static bool has_duplicate_char(
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
