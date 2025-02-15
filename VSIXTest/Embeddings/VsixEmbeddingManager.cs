using System.Collections.Generic;
using System;
using SharedClasses.Models;
using System.Linq;

namespace VSIXTest.Embeddings
{
    public class EmbeddingManager
    {
        public List<CodeSnippet> FindSimilarCodeSnippets(VsixEmbedding promptEmbedding, List<VsixEmbedding> codeEmbeddings, int numberOfSnippetsToReturn = 3)
        {
            var similarSnippets = new List<CodeSnippet>();

            for (int i = 0; i < codeEmbeddings.Count; i++)
            {
                var codeEmbedding = codeEmbeddings[i];
                var similarity = CalculateCosineSimilarity(promptEmbedding.Value, codeEmbedding.Value);

                var snippet = new CodeSnippet
                {
                    Embedding = codeEmbedding.Value,
                    Code = codeEmbedding.Code,
                    Filename = codeEmbedding.Filename,
                    LineNumber = codeEmbedding.LineNumber,
                    Namespace = codeEmbedding.Namespace,
                    Class = codeEmbedding.Class,

                };

                if (similarSnippets.Count < numberOfSnippetsToReturn)
                {
                    similarSnippets.Add(snippet);
                }
                else if (similarity > similarSnippets.Min(s => CalculateCosineSimilarity(promptEmbedding.Value, s.Embedding)))
                {
                    similarSnippets.Remove(similarSnippets.OrderBy(s => CalculateCosineSimilarity(promptEmbedding.Value, s.Embedding)).First());
                    similarSnippets.Add(snippet);
                }
            }

            return similarSnippets.OrderByDescending(s => CalculateCosineSimilarity(promptEmbedding.Value, s.Embedding)).ToList();
        }

        private float CalculateCosineSimilarity(List<float> embedding1, List<float> embedding2)
        {
            if (embedding1.Count != embedding2.Count)
            {
                throw new ArgumentException("Embeddings must have the same dimension.  You're probably using an embeddings file which is wrong for the current embedding model...");
            }

            float dotProduct = 0;
            float magnitude1 = 0;
            float magnitude2 = 0;

            for (int i = 0; i < embedding1.Count; i++)
            {
                dotProduct += embedding1[i] * embedding2[i];
                magnitude1 += embedding1[i] * embedding1[i];
                magnitude2 += embedding2[i] * embedding2[i];
            }

            magnitude1 = (float)Math.Sqrt(magnitude1);
            magnitude2 = (float)Math.Sqrt(magnitude2);

            if (magnitude1 == 0 || magnitude2 == 0)
            {
                return 0;
            }
            else
            {
                return dotProduct / (magnitude1 * magnitude2);
            }
        }
    }
}