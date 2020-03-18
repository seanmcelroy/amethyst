using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class Entity
{
    private static readonly Random Random = new Random(Environment.TickCount);

    public string NameSingular { get; set; }
    public string NamePlural { get; set; }
    public string KeyField { get; set; }
    public string NameField { get; set; }
    public string Feature { get; set; }

    public static Dictionary<string, object> GenerateMock(string entityNameSingular, Dictionary<Entity, EntityField[]> entitiesAndFields, TextWriter errorOutput, int depth = 0)
    {
        var mockRecord = new Dictionary<string, object>();
        int? firstRandomChoice = null;

        foreach (var field in entitiesAndFields.Single(k => string.Compare(k.Key.NameSingular, entityNameSingular) == 0).Value)
        {
            var sbMockDatum = new StringBuilder();
            if (string.IsNullOrWhiteSpace(field.MockValueProvider))
            {
                if (field.Required)
                {
                    var typeMatchesCustomEntity = entitiesAndFields.FirstOrDefault(e => string.Compare(e.Key.NameSingular, field.Type, true) == 0);
                    if (typeMatchesCustomEntity.Key == null)
                    {
                        errorOutput.WriteLine($"[WARN] Entity {entityNameSingular} has no MockValueProvider defined for required field {field.Key}");
                        sbMockDatum.Append("MISSING MockValueProvider definition!");
                    }
                    else if (depth > 3)
                    {
                        errorOutput.WriteLine($"[WARN] Maximum depth reached on mock object graph construction of entity {entityNameSingular} at field {field.Key}");
                        continue;
                    }
                    else
                    {
                        var mock = GenerateMock(typeMatchesCustomEntity.Key.NameSingular, entitiesAndFields, errorOutput, depth + 1);
                        var sMock = System.Text.Json.JsonSerializer.Serialize(mock);
                        sbMockDatum.Append(sMock);
                    }
                }
                else
                    continue;
            }
            else
            {
                var mockParts = field.MockValueProvider.Split(';');
                foreach (var part in mockParts)
                {
                    if (part.StartsWith("random-choice:"))
                    {
                        var bank = part.Substring("random-choice:".Length).Split(',');
                        if (firstRandomChoice == null)
                            firstRandomChoice = Random.Next(0, bank.Length);

                        var choice = firstRandomChoice.Value < bank.Length ? firstRandomChoice.Value : Random.Next(0, bank.Length);
                        sbMockDatum.Append(bank[choice]);
                    }
                    else if (part.StartsWith("text:"))
                        sbMockDatum.Append(part.Substring("text:".Length));
                    else if (part.StartsWith("random-number:"))
                    {
                        var bank = part.Substring("random-number:".Length).Split('-');
                        sbMockDatum.Append(Random.Next(
                            (int.TryParse(bank[0], out int min) ? min : 0),
                            (int.TryParse(bank[1], out int max) ? max : 1000000)));
                    }
                }
                if (sbMockDatum.Length == 0)
                    sbMockDatum.Append("MOCK_DATA");
            }

            mockRecord.Add(field.Key, sbMockDatum.ToString());
        }
        return mockRecord;
    }

    public IEnumerable<string> Validate(IEnumerable<Entity> otherEntities, IEnumerable<EntityField> fields)
    {
        if (string.IsNullOrWhiteSpace(NameSingular))
            yield return $"Missing {nameof(NameSingular)} field";
        if (string.IsNullOrWhiteSpace(NamePlural))
            yield return $"Missing {nameof(NamePlural)} field";
        if (string.IsNullOrWhiteSpace(KeyField))
            yield return $"Missing {nameof(KeyField)} field";
        if (string.IsNullOrWhiteSpace(NameField))
            yield return $"Missing {nameof(NameField)} field";
    }
}