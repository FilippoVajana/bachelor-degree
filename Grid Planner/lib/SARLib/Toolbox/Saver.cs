using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SARLib.Toolbox
{
    public class Saver
    {
        ///// <summary>
        ///// Serializza l'oggetto usando l'hash come nome file
        ///// </summary>
        ///// <param name="obj"></param>
        ///// <param name="destinationPath"></param>
        ///// <param name="extension"></param>
        ///// <returns></returns>
        //public static string SaveToFile(object obj, string destinationPath, string extension) //estrarre in classe dedicata
        //{
        //    var model = obj;

        //    //serializzo l'istanza corrente della classe
        //    string json = JsonConvert.SerializeObject(model);

        //    //creo la cartella di destinazione
        //    var outputDir = Directory.CreateDirectory(Path.Combine(destinationPath, "Output", $"{model.GetType().Name}"));

        //    //calcolo hash del file
        //    var hashFunc = System.Security.Cryptography.MD5.Create();
        //    var stringBuffer = Encoding.ASCII.GetBytes(json);
        //    byte[] hashValue = hashFunc.ComputeHash(stringBuffer);

        //    //creo il file di output
        //    var outFileName = $"{BitConverter.ToString(hashValue).Replace("-", "")}_{model.GetType().Name}{extension}";
        //    string outputFilePath = Path.Combine(outputDir.FullName, outFileName); //$"{outputDir.FullName}\\{outFileName}";
        //    File.WriteAllText(outputFilePath, json, Encoding.ASCII);

        //    return outputFilePath;//path del file appena creato
        //}

        public static string SaveToJsonFile(object obj, string destination, string fileName = null)
        {
            var model = obj;

            //serializzo l'istanza corrente della classe
            string json = JsonConvert.SerializeObject(model);

            //definisco file name
            if (fileName == null)
            {
                //calcolo hash del file
                var hashFunc = System.Security.Cryptography.MD5.Create();
                var stringBuffer = Encoding.ASCII.GetBytes(json);
                byte[] hashValue = hashFunc.ComputeHash(stringBuffer);

                fileName = BitConverter.ToString(hashValue).Replace("-", "");
            }
            
            fileName = $"{fileName}.json";
            destination = Path.Combine(destination, fileName); 

            //salvo il file
            File.WriteAllText(destination, json, Encoding.ASCII);

            return destination;
        }
    }
}
