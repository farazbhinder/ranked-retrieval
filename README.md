# ranked-retrieval
A simple ranked retrieval search engine using cosine similarity between documents and given query to rank documents

Calculates TF*IDF score of each term in a document and creates a term docuent vector for each document, and prints the top five closest matching documents in vector space(in rank order) using cosine similariy measure

The dataset I am using is following
http://www.cs.cmu.edu/afs/cs/project/theo-20/www/data/news20.tar.gz


### How to run the program?
open cmd

enter

ConsoleApplication3 "query" "fullpath to the folder"

for e.g.

ConsoleApplication3 "England Pakistan Canada" "C:\20_newsgroups"