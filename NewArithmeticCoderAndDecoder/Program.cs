using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewArithmeticCoderAndDecoder
{
    class Program
    {
        public static Encoding enc { get; set; }
        public static Dictionary<byte, double> AlphabetAndPropabilities { get; set; } = new Dictionary<byte, double>();//алфавит и вероятности
        public static Dictionary<byte, (double, double)> AlphabetProbCumulativeProb { get; set; } = new Dictionary<byte, (double, double)>();//алфавит, вероятности и кумул вероятности
        public static Dictionary<string, (List<byte>, List<byte>)> BlocksAndTheirCodeWords { get; set; }
            = new Dictionary<string, (List<byte>, List<byte>)>();//ключ - суперсимвол(массив байт в стринге), суперсимвол в списке,
        //кодовое слово для суперсимвола в списке
        public static List<byte> ListBinaryToTransform { get; set; } = new List<byte>();//список для преобразования расширением BitArray для записи в файл
        public static uint NumberOfBlocksToDecode { get; set; } = 0;//количество суперсимволов в тексте, сколько нужно декодировать
        public static Dictionary<string, string> CodewordsAndTheirRepresentation { get; set; } = new Dictionary<string, string>();
        //первый стринг - кодовое слово, второй - его представление в аски, неободим для проверки совпадений в кодовых словах
        public static byte NumberOfNullsInLastBlock { get; set; } = 0;//количество нулей в посл блоке, для корректного считывания
        public static List<byte> LenghtOfWords { get; set; } = new List<byte>();//длины код слов
        public static List<List<byte>> CodeWords { get; set; } = new List<List<byte>>();//матрица код слов

        static void Main(string[] args)
        {
            if(args[3] == "1")
            {
                var OutputFileList = new List<byte>();//текст из сжатого файла
                FileStream fileStream;
                FileInfo fileInfo;

                using (fileStream = File.OpenRead(args[0]))
                {
                    var fileSize = fileStream.Length;
                    byte[] buffer = new byte[fileSize];

                    fileInfo = new FileInfo(args[0]);
                    fileStream.Read(buffer, 0, Convert.ToInt32(fileSize));
                    OutputFileList = new List<byte>(buffer);

                }

                ArithmeticCodeBuilder(OutputFileList, Convert.ToInt32(Convert.ToInt32(args[1])));
                var Output = new List<byte>();


                Output.Add((byte)(AlphabetAndPropabilities.Count - 1));//количество символов в алфавите -1 чтобы не было ошибки
                
                Output.Add(Convert.ToByte(Convert.ToInt32(args[1])-1));//добавляем размер блока, теоретически от 2 до 255


                uint numOfSupersymbols = (uint)BlocksAndTheirCodeWords.Count;
                Output.AddRange(BitConverter.GetBytes(numOfSupersymbols));//количество суперсимволов(букв алфавита)

                Output.AddRange(BitConverter.GetBytes(NumberOfBlocksToDecode));//количество суперсимволов в тексте, сколько нужно декодировать

                Output.Add(NumberOfNullsInLastBlock);//добавляем количество нулей в последнем блоке


                for (int i = 0; i < AlphabetProbCumulativeProb.Count; i++)
                {
                    Output.Add(AlphabetProbCumulativeProb.ElementAt(i).Key);//добавляем символ алфавита
                    Output.AddRange(BitConverter.GetBytes(AlphabetProbCumulativeProb.ElementAt(i).Value.Item1));//вероятность символа
                }
                //var d = BitConverter.ToDouble(array.GetRange(1,8).ToArray(),0);
                var symbLenght = 0;
                for (int i = 0; i < BlocksAndTheirCodeWords.Count; i++)
                {
                    symbLenght = BlocksAndTheirCodeWords.ElementAt(i).Value.Item2.Count;
                    if (symbLenght < 256)
                    {
                        Output.Add((byte)symbLenght);
                    }
                    else
                    {
                        //error
                    }
                }


                var listOfAllArithmeticWords = new List<byte>();//склеиваем все кодовые слова вместе
                for (int i = 0; i < BlocksAndTheirCodeWords.Count; i++)//добавляем все кодовые слов
                {
                    listOfAllArithmeticWords.AddRange(BlocksAndTheirCodeWords.ElementAt(i).Value.Item2);
                }

                listOfAllArithmeticWords.AddRange(ListBinaryToTransform);//склеиваем кодовые слова и текст из кодовых слов для трансформации в битовый массив

                var wordsAfterConvertion = listOfAllArithmeticWords.ToBitArray(listOfAllArithmeticWords.Count);//трансформируем в BitArray
                var byteArrayWords = wordsAfterConvertion.BitArrayToByteArray();//BitArray в массив байт

                //var reverse = byteArrayWords.ByteArrayToBitList();

                Output.AddRange(byteArrayWords);

                using (FileStream fs = File.Create(args[2]))
                {
                    fs.Write(Output.ToArray(), 0, Output.Count);
                }
            }
            else
            {
                var OutputFileList = new List<byte>();//текст из сжатого файла
                FileStream fileStream;
                FileInfo fileInfo;

                using (fileStream = File.OpenRead(args[2]))
                {
                    var fileSize = fileStream.Length;
                    byte[] buffer = new byte[fileSize];

                    fileInfo = new FileInfo(args[0]);
                    fileStream.Read(buffer, 0, Convert.ToInt32(fileSize));
                    OutputFileList = new List<byte>(buffer);

                }

                var numberOfAlphabetSymbols = OutputFileList[0] + 1;//количество символов в алфавите
                var sizeOfBlock = OutputFileList[1] + 1;//размер суперсимвола
                var numberSupersymbols = BitConverter.ToUInt32(OutputFileList.GetRange(2, 4).ToArray(), 0);//количество букв(суперсимволов) в алфавите
                var numberOfBlocksToDecode = BitConverter.ToUInt32(OutputFileList.GetRange(6, 4).ToArray(), 0);//количество всего блоков для декодирования
                var numbersOfNulls = OutputFileList[10];//количество нулей в последнем блоке

                OutputFileList.RemoveRange(0, 11);

                AlphabetAndPropabilities = new Dictionary<byte, double>();
                int j = 0;
                for (int i = 0; i < numberOfAlphabetSymbols; i++)
                {

                    var block = OutputFileList.GetRange(j, 9);
                    AlphabetAndPropabilities.Add(block[0], BitConverter.ToDouble(block.GetRange(1, 8).ToArray(), 0));
                    j += (8 + 1);

                }

                OutputFileList.RemoveRange(0, j);

                for (int i = 0; i < numberSupersymbols; i++)
                {
                    LenghtOfWords.Add(OutputFileList[i]);
                }
                OutputFileList.RemoveRange(0, (int)numberSupersymbols);

                OutputFileList = new List<byte>(OutputFileList.ByteListToBitList());

                var textToDecode = GettingArithmeticWordsFromData(OutputFileList, numberSupersymbols, LenghtOfWords);

                //AlphabetAndPropabilities.Add(1, 0.1);
                //AlphabetAndPropabilities.Add(2, 0.6);
                //AlphabetAndPropabilities.Add(3, 0.3);

                GettingCumulativeProbability(AlphabetAndPropabilities);//снова получаем вероятности для нахождения букв исходного текста

                List<byte> inputText = GettingDataFromArithmeticWords(textToDecode, numberOfBlocksToDecode, sizeOfBlock);

                inputText.RemoveRange(inputText.Count - numbersOfNulls, numbersOfNulls);
                
            }


        }

        public static List<byte> GettingDataFromArithmeticWords(List<byte> byteArray, uint numberOfWords, int sizeOfBlock)
        {
            List<byte> DataList = new List<byte>();
            int iterator = 0;
            int found = 0;
            List<byte> newWord = new List<byte>();
            List<int> positionsOfCoincidenceWords = new List<int>();

            var longestWord = LenghtOfWords.Max();

            for (int m = 0; m < byteArray.Count; m++)
            {
                var ret = 0;
                if (iterator != numberOfWords)
                {
                    if (m + 1 + longestWord <= byteArray.Count)
                    {
                        newWord = byteArray.GetRange(m, longestWord);
                    }
                    else
                    {
                        var a = byteArray.Count - m;
                        newWord = byteArray.GetRange(m, a);

                    }

                    for (int i = 0; i < longestWord && ret == 0; i++)//for (int i = longestWord; i > 0 || ret == 1; i--)
                    {
                        for (int j = 0; j < CodeWords.Count; j++)//for (int j = CodeWords.Count; j > 0; j--)
                        {
                            try
                            {
                                if (newWord.SequenceEqual(CodeWords[j]))
                                {
                                    //List<byte> DataList = new List<byte>();//можно сразу весь текст сюда считывать
                                    List<byte> byteArrayRecursion = new List<byte>();//обнулять при каждом вызове

                                    var doubleRes = ConvertBinaryToDouble(newWord);
                                    ReverseGettingCumulativeProbabilityRecursion(DataList, byteArrayRecursion, doubleRes, 0, sizeOfBlock);//чтобы рекурсия дошла до конца передаем на 1 эл больше
                                    
                                    //ReverseGettingCumulativeProbabilityRecursion(DataList, doubleRes, 0, newWord.Count - 1);

                                    iterator += 1;
                                    ret = 1;
                                    m += newWord.Count - 1;
                                    newWord.Clear();
                                    break;
                                }
                            }
                            catch
                            {

                            }

                        }
                        if (ret == 0)
                            newWord.RemoveAt(newWord.Count - 1);

                    }
                }
                else
                {
                    break;
                }


            }


            //Recursion(iterator, byteArray.Count, ref positionsOfCoincidenceWords, ref newWord, byteArray, false, 0);

            return DataList;
        }


        public static double ConvertBinaryToDouble(List<byte> binary)
        {
            var res = 0.0;
            for (int i = 0; i < binary.Count; i++)
            {
                res += binary[i] * Math.Pow(0.5, i + 1);
            }
            return res;
        }

        public static List<byte> GettingArithmeticWordsFromData(List<byte> byteArray, uint alphabetSize, List<byte> LenghtOfWords)
        {
            int index = 0;
            CodeWords = new List<List<byte>>();
            for (int i = 0; i < alphabetSize; i++)
            {
                CodeWords.Add(new List<byte>());
                CodeWords[i].AddRange(byteArray.GetRange(index, LenghtOfWords[i]));
                index += LenghtOfWords[i];
            }

            byteArray.RemoveRange(0, index);
            return byteArray;
        }

        public static void ArithmeticCodeBuilder(List<byte> OutputFileList, int nSymbolsInBlocks)//число от 1 до 255
        {
            enc = Encoding.ASCII;

            NumberOfNullsInLastBlock = (byte)(OutputFileList.Count % nSymbolsInBlocks);

            if (NumberOfNullsInLastBlock != 0)
            {
                for (int l = 0; l < nSymbolsInBlocks - NumberOfNullsInLastBlock; l++)
                {
                    OutputFileList.Add(48);//48 это ноль
                }
            }

            NumberOfNullsInLastBlock = (byte)(nSymbolsInBlocks - NumberOfNullsInLastBlock);

            GettingAlphabetAndProbabilities(OutputFileList);
            GettingCumulativeProbability(AlphabetAndPropabilities);

            for (int i = 0; i < OutputFileList.Count; i += nSymbolsInBlocks)
            {
                if ((i + nSymbolsInBlocks) > OutputFileList.Count)
                {
                    //var list = OutputFileList.GetRange(i, OutputFileList.Count - i);
                    //NumberOfNullsInLastBlock = (byte)(nSymbolsInBlocks - list.Count);

                    //var listNulls = new List<byte>();
                    //for (int l = 0; l < NumberOfNullsInLastBlock; l++)
                    //{
                    //    listNulls.Add(0);
                    //}
                    //list.AddRange(listNulls);

                    //CreatingArithmeticWord(list);
                }
                else
                {
                    var list = OutputFileList.GetRange(i, nSymbolsInBlocks);
                    CreatingArithmeticWord(list);
                }

            }

        }
        public static void CreatingArithmeticWord(List<byte> ListToEncode)
        {
            if (BlocksAndTheirCodeWords.ContainsKey(enc.GetString(ListToEncode.ToArray())))
            {
                var key = enc.GetString(ListToEncode.ToArray());
                ListBinaryToTransform.AddRange(BlocksAndTheirCodeWords[key].Item2);
                NumberOfBlocksToDecode += 1;
            }
            else
            {
                var blockCumulativeprobability = GettingCumulativeProbabilityRecursion(ListToEncode, 0, ListToEncode.Count - 1);
                var Gresult = GettingProbabilityRecursion(ListToEncode, 0, ListToEncode.Count - 1);

                var lenghtCodeWord = Math.Ceiling(Math.Abs(Math.Log(Gresult, 2)) + 1);//длина кодового слова

                var wordToConvert = blockCumulativeprobability + Gresult / 2;//кодовое слово в десятичном представлении

                var output = printBinary(wordToConvert);
                var trimmedOutput = output.Substring(0, (int)lenghtCodeWord);

                var codeword = new List<byte>();
                var list = output.ToArray();
                for (int i = 0; i < lenghtCodeWord; i++)
                {
                    var d = list[i].ToString();
                    codeword.Add(Byte.Parse(d));
                }
                var m = enc.GetString(ListToEncode.ToArray());


                //сделать проверку на уже существующие слова
                if (CodewordsAndTheirRepresentation.ContainsKey(trimmedOutput))
                {
                    //error
                }
                else
                {
                    CodewordsAndTheirRepresentation.Add(trimmedOutput, enc.GetString(ListToEncode.ToArray()));
                    BlocksAndTheirCodeWords.Add((enc.GetString(ListToEncode.ToArray())), (ListToEncode, codeword));
                    ListBinaryToTransform.AddRange(codeword);
                    NumberOfBlocksToDecode += 1;
                }
            }

        }

        public static String printBinary(double num)
        {
            // Check Number is Between 0 to 1 or Not  
            if (num >= 1 || num <= 0)
                return "ERROR";

            StringBuilder binary = new StringBuilder();
            //binary.Append(".");

            while (num > 0)
            {
                if (binary.Length >= 100)
                    return binary.ToString();

                double r = num * 2;
                if (r >= 1)
                {
                    binary.Append(1);
                    num = r - 1;
                }
                else
                {
                    binary.Append(0);
                    num = r;
                }
            }
            if (binary.Length < 100)
            {
                char[] vs = new char[100 - binary.Length];
                for (int i = 0; i < vs.Length; i++)
                {
                    vs[i] = '0';

                }
                binary.Append(vs);
            }
            return binary.ToString();
        }

        public static void GettingAlphabetAndProbabilities(List<byte> byteArray)
        {
            var numberOfSymbolsInArray = byteArray.Count;
            double countSymbols;//сколько раз встречается тот или иной элемент
            double propability;
            for (int i = 0; i < numberOfSymbolsInArray; i++)
            {
                if (AlphabetAndPropabilities.ContainsKey(byteArray[i]))
                {
                    continue;
                }
                else
                {
                    countSymbols = byteArray.Where(x => x.Equals(byteArray[i])).Count();
                    propability = countSymbols / numberOfSymbolsInArray;
                    AlphabetAndPropabilities.Add(byteArray[i], propability);
                }
            }
        }

        public static void GettingCumulativeProbability(Dictionary<byte, double> alphAndProb)
        {
            var sorted = alphAndProb.OrderBy(obj => obj.Key).ToDictionary(obj => obj.Key, obj => obj.Value);

            for (int i = 0; i < sorted.Count; i++)
            {
                if (i == 0)
                {
                    //sorted.ElementAt(i)
                    var tuple = (sorted.ElementAt(i).Key, sorted.ElementAt(i).Value);
                    AlphabetProbCumulativeProb.Add(sorted.ElementAt(i).Key, (sorted.ElementAt(i).Value, 0.0));
                }
                else
                {

                    var cumulatProp = AlphabetProbCumulativeProb.ElementAt(i - 1).Value.Item1
                        + AlphabetProbCumulativeProb.ElementAt(i - 1).Value.Item2;

                    AlphabetProbCumulativeProb.Add(sorted.ElementAt(i).Key, (sorted.ElementAt(i).Value, cumulatProp));
                }
            }
        }

        public static double GettingCumulativeProbabilityRecursion(List<byte> byteArray, int firstPosition, int secondPosition)
        {
            if (secondPosition == firstPosition)
            {
                var a = AlphabetProbCumulativeProb[byteArray[secondPosition]].Item2;
                return a;
            }
            else
            {
                var a = GettingCumulativeProbabilityRecursion(byteArray, firstPosition, secondPosition - 1)
                    + GettingProbabilityRecursion(byteArray, firstPosition, secondPosition - 1)
                    * AlphabetProbCumulativeProb[byteArray[secondPosition]].Item2;
                return a;
            }
        }

        public static double GettingProbabilityRecursion(List<byte> byteArray, int firstPosition, int secondPosition)
        {
            if (secondPosition == firstPosition)
            {
                var a = AlphabetAndPropabilities[byteArray[secondPosition]];
                return a;
            }
            else
            {
                var a = GettingProbabilityRecursion(byteArray, firstPosition, secondPosition - 1) *
                    AlphabetAndPropabilities[byteArray[secondPosition]];
                return a;
            }
        }



        public static double ReverseGettingCumulativeProbabilityRecursion(List<byte> DataList, List<byte> byteArray, double res, int firstPosition, int secondPosition)
        {
            if (secondPosition == firstPosition)
            {
                //var a = AlphabetProbCumulativeProb[byteArray[secondPosition]].Item2;
                //return a;
                return 0.0; //S
            }
            else
            {
                var S = ReverseGettingCumulativeProbabilityRecursion(DataList, byteArray, res, firstPosition, secondPosition - 1);
                var G = ReverseGettingProbabilityRecursion(DataList, byteArray, firstPosition, secondPosition - 1);
                
                var resultSqG = 0.0;

                for (int i = 0; i < AlphabetProbCumulativeProb.Count; i++)
                {
                    resultSqG = S + AlphabetProbCumulativeProb.ElementAt(i).Value.Item2 * G;
                    if(resultSqG < res)
                    {
                        if(i + 1 == AlphabetProbCumulativeProb.Count)
                        {
                            DataList.Add(AlphabetProbCumulativeProb.ElementAt(i).Key);
                            byteArray.Add(AlphabetProbCumulativeProb.ElementAt(i).Key);
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if(i == 0)
                        {
                            //error
                        }
                        DataList.Add(AlphabetProbCumulativeProb.ElementAt(i - 1).Key);
                        byteArray.Add(AlphabetProbCumulativeProb.ElementAt(i - 1).Key);
                        resultSqG = S + AlphabetProbCumulativeProb.ElementAt(i-1).Value.Item2 * G;
                        break;
                    }
                }
                   // * AlphabetProbCumulativeProb[byteArray[secondPosition]].Item2;
                return resultSqG;
            }
        }

        public static double ReverseGettingProbabilityRecursion(List<byte> DataList, List<byte> byteArray, int firstPosition, int secondPosition)
        {
            if (secondPosition == firstPosition)
            {
                //var a = AlphabetAndPropabilities[byteArray[secondPosition]];
                //return a;
                return 1.0;//G
            }
            else
            {
                var a = ReverseGettingProbabilityRecursion(DataList, byteArray, firstPosition, secondPosition - 1) *
                    AlphabetAndPropabilities[byteArray[secondPosition - 1]];
                return a;
            }
        }
    }
}
