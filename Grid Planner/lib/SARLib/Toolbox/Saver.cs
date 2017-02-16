using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SARLib.Toolbox
{
    class Saver
    {
        public static string SaveToFile(object obj, string destinationPath, string extension) //estrarre in classe dedicata
        {
            var model = obj;

            //serializzo l'istanza corrente della classe
            string json = JsonConvert.SerializeObject(model);

            //creo la cartella di destinazione
            var outputDir = Directory.CreateDirectory(System.IO.Path.Combine(destinationPath, "Output", $"{model.GetType().Name}"));

            //calcolo hash del file
            var hashFunc = System.Security.Cryptography.MD5.Create();
            var stringBuffer = Encoding.ASCII.GetBytes(json);
            byte[] hashValue = hashFunc.ComputeHash(stringBuffer);

            //creo il file di output
            var outFileName = $"{BitConverter.ToString(hashValue).Replace("-", "")}_{model.GetType().Name}{extension}";
            string outputFilePath = System.IO.Path.Combine(outputDir.FullName, outFileName); //$"{outputDir.FullName}\\{outFileName}";
            File.WriteAllText(outputFilePath, json, Encoding.ASCII);

            return outputFilePath;//path del file appena creato
        }
    }
}
