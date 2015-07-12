using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

/*

    Faraz Ahmad

    Cosine Similarity

*/

namespace ConsoleApplication3
{
    class DocScorePair
    {
        public string DocName { get; set; }
        public double Score { get; set; }
    }
    class Program
    {
		//we tokenize the words on these punctuations
        static char[] punctuations = new Char[] { ' ','\t','\n', ',', '.', ';', '^', '`', ':', '?', '&', '!', '+', '-', '_', '#', '<', '>', '/', '|', '\\', '"', '(', ')', '[', ']', '=', '*', '%', '\t' };


        //initial dictionary - it stores the term frequencies...
        //format is Dictionary<term x <doc y, tf>>
        static Dictionary<string, Dictionary<string, int>> dt = new Dictionary<string, Dictionary<string, int>>();

        //dictionary which stores the term vectors for each document
        //format is Dictionary<doc x <term y, tf*idf>>
        static Dictionary<string, Dictionary<string, double>> dtvec = new Dictionary<string, Dictionary<string, double>>();

        //dictionary which stores the term vectors for query
        //format is Dictionary<query <term y, tf*idf>>
        static Dictionary<string, double> qtvec = new Dictionary<string, double>();

        //dictionary which stores the document scores of each of the document in which any term exists
        static Dictionary<string, double> documentScores = new Dictionary<string, double>();

        static HashSet<string> allDocuments = new HashSet<string>();

        const int THRESHOLD = 25000;
        
        static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                return 0;
            }

            string query = args[0];
            string path = @args[1];


            Stopwatch stopWatch = new Stopwatch();
            try
            {

                stopWatch.Start();
                MakeIndex(path);
                stopWatch.Stop();
                //Console.WriteLine(stopWatch.Elapsed);

                stopWatch.Reset();
                stopWatch.Start();
                MakeDocumentTermVector();
                stopWatch.Stop();
                //Console.WriteLine(stopWatch.Elapsed);



                stopWatch.Reset();
                stopWatch.Start();
                RankTheDocs(query);
                stopWatch.Stop();
                //Console.WriteLine(stopWatch.Elapsed);

            }
            catch (System.Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }



            Console.ReadLine();
            return 0;
        }

        static void MakeIndex(string sDir)
        {
            //using stop words we donot index these, which helps in cutting down the dimensionality of the document-term vecs
            HashSet<string> stopwordslist = GetStopWords();

            Dictionary<string, int> tokensOccurences = new Dictionary<string, int>();
            foreach (string filename in Directory.EnumerateFiles(sDir, "*.*", SearchOption.AllDirectories))
            {
                //foreach (string filename in Directory.GetFiles(d))
                //{
                    StreamReader reader = File.OpenText(filename);
                    allDocuments.Add(filename);
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] items = line.Split(punctuations);
                        foreach (string s in items)
                        {
                            if (string.IsNullOrWhiteSpace(s))
                            {
                                continue;
                            }
                            //make a term from this token...
                            string st = s.ToLower();
                            st = GetStemmedTerm(st);

                            //dont index the stop words and numbers, hence cutting down the dimensionality of the doc-term vectors when we make them
                            if (!stopwordslist.Contains(st) && !Regex.IsMatch(st, @"^\d+$"))
                            {
                                if (dt.ContainsKey(st))
                                {
                                    if (dt[st].ContainsKey(filename))
                                    {
                                        dt[st][filename] = dt[st][filename] + 1;
                                    }
                                    else
                                    {
                                        dt[st].Add(filename, 1);
                                    }

                                }
                                else
                                {
                                    Dictionary<string, int> vec = new Dictionary<string,int>();
                                    vec.Add(filename, 1);
                                    dt.Add(st, vec);
                                }

                            }
                        }

                    }
                    reader.Close();

                //}
            }
            var sortedDict = from entry in tokensOccurences orderby entry.Value descending select entry;
            Dictionary<string, int> tokenOccurences2 = sortedDict.ToDictionary(p => p.Key, p => p.Value);

            int i = 0;
            //frequency thresholding
            foreach (KeyValuePair<string, int> kvp in tokenOccurences2)
            {
                if (kvp.Value > THRESHOLD)
                {
                    dt.Remove(kvp.Key);
                    i++;
                }
                else
                {
                    break;
                }
            }
        }


        //•	Create Term-document vectors for each document
        static void MakeDocumentTermVector()
        {
            foreach (string tkey in dt.Keys)
            {
                foreach (KeyValuePair<string, int> pair in dt[tkey])
                {
                    int df = dt[tkey].Count;
                    double idf = (double) Math.Log( allDocuments.Count / df);
                    //cut down dimensionality of the vectors, i.e. do not use terms that donot occur in the doc, or infrequently
                    if (pair.Value > 0) // if tf > 0
                    {
                        //make tf*idf weight in the document term vec
                        if (!dtvec.ContainsKey(pair.Key))
                        {
                            Dictionary<string, double> temp = new Dictionary<string, double>();
                            temp.Add(tkey, pair.Value*idf);
                            dtvec.Add(pair.Key, temp);
                        }
                        else
                        {
                            dtvec[pair.Key].Add(tkey, pair.Value*idf);
                        }   
                    }
                    
                }
            }
        }

        static void MakeQueryTermVector(string query)
        {
            HashSet<string> stopwordslist = GetStopWords();
            Dictionary<string, int> temp = new Dictionary<string, int>();
            //Dictionary<string, int> results = new Dictionary<string, int>();
            string[] words = query.Split(punctuations);
            foreach (string item in words)
            {
                string st = item.ToLower();
                st = GetStemmedTerm(st);
                if (string.IsNullOrWhiteSpace(st))
                {
                    continue;
                }
                if (!stopwordslist.Contains(st) && !Regex.IsMatch(st, @"^\d+$"))
                {
                    if (temp.ContainsKey(st))
                    {
                        temp[st] = temp[st] + 1;
                    }
                    else
                    {
                        temp.Add(st, 1);
                    }
                }
            }

            //make tf*idf weigth in the query term vec
            foreach (KeyValuePair<string, int> pair in temp)
            {
                int df = dt[pair.Key].Count;
                double idf = (double) Math.Log( allDocuments.Count / df);
                qtvec.Add(pair.Key, (double)pair.Value * idf);
            }
        }

        //calculate the cosine similarity measure of given doc with the query.
        static double CalculateScoreOfThisDoc(string docId)
        {
            double score = 0;
            foreach(string qkey in qtvec.Keys)
            {
                if (dtvec[docId].ContainsKey(qkey))
                {
                    score = score + qtvec[qkey] * dtvec[docId][qkey];   
                }
            }


            //divide the dot product of query and doc by euclidian lengths of query and doc
            double euclidianWeightOfQuery = 0;
            foreach (KeyValuePair<string, double> pair in qtvec)
            {
                euclidianWeightOfQuery = euclidianWeightOfQuery + pair.Value * pair.Value;
            }
            euclidianWeightOfQuery = Math.Sqrt(euclidianWeightOfQuery);

            double euclidianWeightOfDoc = 0;
            foreach (KeyValuePair<string, double> pair in dtvec[docId])
            {
                euclidianWeightOfDoc = euclidianWeightOfDoc + pair.Value * pair.Value;
            }
            euclidianWeightOfDoc = Math.Sqrt(euclidianWeightOfDoc);

            score = score / (euclidianWeightOfQuery * euclidianWeightOfDoc);
            return score;
        }

        static void RankTheDocs(string query)
        {
            MakeQueryTermVector(query);

            HashSet<string> docsWhichContainAnyQueryTerm = new HashSet<string>();
            foreach (string queryterm in qtvec.Keys)
            {
                if (dt.ContainsKey(queryterm))
	            {
		            foreach (string doc in dt[queryterm].Keys)
                    {
                        docsWhichContainAnyQueryTerm.Add(doc);
                    }
	            }
                
            }

            //only calculate score of those docs in which the query term exists! no need not calcualte score of all the 20k docs
            foreach (string doc in docsWhichContainAnyQueryTerm)
            {
                documentScores.Add(doc, CalculateScoreOfThisDoc(doc));
            }

            var sortedDict = from entry in documentScores orderby entry.Value descending select entry;
            //Dictionary<string, double> documentScoresSorted = sortedDict.ToDictionary(p => p.Key, p => p.Value);


            List<KeyValuePair<string, double>> dslist = sortedDict.ToList();



            //Console.WriteLine("Found {0} results \n", dslist.Count);
            //•	Print the top 5 closest matching documents in vector space (in rank order), using the cosine similarity measure.
            for (int i = 0; i < 5; i++)
            {
                string fullfilepath = dslist[i].Key.ToString();
                string[] filesfoldersnames = fullfilepath.Split(new char[] { '\\' });
                string pathIn20NewsgroupFolder = "";
                for (int j = filesfoldersnames.Length-2; j < filesfoldersnames.Length; j++)
                {
                    pathIn20NewsgroupFolder = pathIn20NewsgroupFolder +"/"+ filesfoldersnames[j];
                }
                
                //Console.WriteLine(dslist[i].ToString());
                Console.WriteLine(pathIn20NewsgroupFolder.Substring(1));
            }

            //foreach (keyvaluepair<string, double> pair in dslist)
            //{
            //    console.writeline(pair.key);
            //    console.writeline("   {0}", pair.value);
            //}


        }

        static HashSet<string> GetStopWords()
        {
            HashSet<string> stopwords = new HashSet<string>();
            StreamReader sr = File.OpenText("stopwords1.txt");
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                { continue; }
                stopwords.Add(line.Trim());
                //Console.WriteLine(line);
            }
            sr.Close();
            return stopwords;

        }

        static string GetStemmedTerm(string st)
        {
            Stemmer stemmer = new Stemmer();
            char[] starr = st.ToCharArray();
            stemmer.add(starr, starr.Length);
            stemmer.stem();
            return stemmer.ToString();
        }
    }
}
