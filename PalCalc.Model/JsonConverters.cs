﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PalCalc.Model.BreedingResult;

namespace PalCalc.Model
{
    public class BreedingResultJsonConverter : JsonConverter<BreedingResult>
    {
        private IEnumerable<Pal> pals;

        public BreedingResultJsonConverter(IEnumerable<Pal> pals)
        {
            this.pals = pals;
        }

        public override BreedingResult ReadJson(JsonReader reader, Type objectType, BreedingResult existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var token = JToken.ReadFrom(reader);
            return new BreedingResult()
            {
                Parent1 = token["Parent1ID"].ToObject<PalId>().ToPal(pals),
                Parent2 = token["Parent2ID"].ToObject<PalId>().ToPal(pals),
                Child = token["ChildID"].ToObject<PalId>().ToPal(pals)
            };
        }

        public override void WriteJson(JsonWriter writer, BreedingResult value, JsonSerializer serializer)
        {
            JToken.FromObject(new
            {
                Parent1ID = value.Parent1.Id,
                Parent2ID = value.Parent2.Id,
                ChildID = value.Child.Id,
            }).WriteTo(writer);
        }
    }

    public class PalDBJsonConverter : JsonConverter<PalDB>
    {
        public override PalDB ReadJson(JsonReader reader, Type objectType, PalDB existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var asToken = JToken.ReadFrom(reader);

            var pals = asToken["Pals"].ToObject<List<Pal>>();
            var traits = asToken["Traits"].ToObject<List<Trait>>();
            var breedingGenderProbability = asToken["BreedingGenderProbability"].ToObject<Dictionary<string, Dictionary<PalGender, float>>>();
            var minBreedingSteps = asToken["MinBreedingSteps"].ToObject<Dictionary<string, Dictionary<string, int>>>();

            serializer.Converters.Add(new BreedingResultJsonConverter(pals));
            var breeding = asToken["Breeding"].ToObject<List<BreedingResult>>(serializer);

            return new PalDB()
            {
                PalsById = pals.ToDictionary(p => p.Id),
                Traits = traits,
                Breeding = breeding,

                MinBreedingSteps = minBreedingSteps.ToDictionary(
                    kvp => kvp.Key.ToPal(pals),
                    kvp => kvp.Value.ToDictionary(
                        ikvp => ikvp.Key.ToPal(pals),
                        ikvp => ikvp.Value
                    )
                ),

                BreedingGenderProbability = breedingGenderProbability.ToDictionary(
                    kvp => kvp.Key.ToPal(pals),
                    kvp => kvp.Value
                ),
            };
        }

        public override void WriteJson(JsonWriter writer, PalDB value, JsonSerializer serializer)
        {
            var breedingResultConverter = new BreedingResultJsonConverter(value.Pals);
            serializer.Converters.Add(breedingResultConverter);

            var breedingToken = JToken.FromObject(value.Breeding, serializer);
            JToken.FromObject(new
            {
                Pals = value.Pals,
                Breeding = breedingToken,
                Traits = value.Traits,
                BreedingGenderProbability = value.BreedingGenderProbability.ToDictionary(kvp => kvp.Key.Name, kvp => kvp.Value),
                MinBreedingSteps = value.MinBreedingSteps.ToDictionary(
                    kvp => kvp.Key.Name,
                    kvp => kvp.Value.ToDictionary(
                        ikvp => ikvp.Key.Name,
                        ikvp => ikvp.Value
                    )
                )
            }).WriteTo(writer, breedingResultConverter);
        }
    }
}
