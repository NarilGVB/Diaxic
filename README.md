This repository is the integration of [this Dialogue Visual Tool](https://narilgvb.itch.io/dialogue-visual-editor) developed by me, to any platform or engine that uses C#. This includes Unity 3D and Godot.

# Diaxic Integration

SavedData -> The Dialogue Visual Tool exports a json file that needs to be deserialize into this class.

Deserialization example:

    public static class JsonSerialization
    {
         private static JsonSerializerSettings SerializerSettings => new JsonSerializerSettings()
         {
             ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
             TypeNameHandling = TypeNameHandling.All,
             Formatting = Formatting.Indented,
             SerializationBinder = new MySerializationBinder()
         };
          
         public static SavedData ReadJson(string json)
         {
             string storyJson = FileAccess.Open(json, FileAccess.ModeFlags.Read).GetAsText();
             return JsonConvert.DeserializeObject<saveddata>(storyJson, SerializerSettings);
         }
    }

CustomBinder -> Some deserialization methods may need a custom binder. OldCustomBinder is a binder for json files created with a version of the Dialogue Visual Tool previous to 0.2.0.

DialogueManager -> Input the resulting SavedData instance to obtain the dialogue flow in order and following the logic used on the Dialogue Visual Tool.
