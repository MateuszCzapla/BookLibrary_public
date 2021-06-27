﻿using System;
using System.Reflection;
using System.IO;
using Microsoft.Data.Sqlite;
using BookLibrary.Models;
using System.Collections.ObjectModel;
using BookLibrary.ViewModels.Books;
using System.Collections.Generic;

namespace BookLibrary.Other
{
    public static class DatabaseOperations
    {
        private const string dbName = "book_library.db";

        public static void CheckDatabase()
        {
            if (!File.Exists(dbName)) DatabaseOperations.CreateDatabase();
            else
            {
                using (var connection = new SqliteConnection("Data Source=" + dbName))
                {
                    connection.Open();
                    //TODO Sprawdzenie struktury bazy danych
                    connection.Close();
                }
            }
        }

        public static void CreateDatabase()
        {
            using (var connection = new SqliteConnection("Data Source=" + dbName))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    CREATE TABLE author (id INTEGER PRIMARY KEY, name TEXT, timestamp INTEGER);
                    CREATE TABLE book (id INTEGER PRIMARY KEY, title TEXT, year INTEGER, timestamp INTEGER);
                    CREATE TABLE author_has_book (author_id INTEGER, book_id INTEGER);
                ";
                command.ExecuteNonQuery();
            }

            DBSampleDataOperations.FillAuthorSampleData(1000);
            DBSampleDataOperations.FillBookSampleData(1);
            DBSampleDataOperations.FillAuthorHasBookSampleData();
        }

        public static ObservableCollection<Book> ReadDataBase(int firstRow, int rowsCount, ref int totalRowsCount)
        {
            ObservableCollection<Book> books = new ObservableCollection<Book>();

            if (!File.Exists(dbName)) return books;

            using (var connection = new SqliteConnection("Data Source=" + dbName))
            {
                connection.Open();

                var command = connection.CreateCommand();

                command.CommandText =@"SELECT COUNT(*) FROM book;";
                using (var reader = command.ExecuteReader())
                {
                    reader.Read();
                    totalRowsCount = reader.GetInt32(0);
                }

                command.CommandText =
                @"
                    SELECT id, title, year, timestamp
                    FROM book LIMIT $firstRow, $rowsCount
                ";
                command.Parameters.AddWithValue("$firstRow", firstRow);
                command.Parameters.AddWithValue("$rowsCount", rowsCount);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        books.Add(new Book(reader.GetInt32(0), reader.GetString(1), 1111));
                    }
                }
            }

            return books;
        }

        public static ObservableCollection<Book> ReadDataBase(int id, string title, int year, string dateFrom, string dateTo)
        {
            ObservableCollection<Book> books = new ObservableCollection<Book>();

            if (!File.Exists(dbName)) return books;

            using (var connection = new SqliteConnection("Data Source=" + dbName))
            {
                connection.Open();

                var command = connection.CreateCommand();

                command.CommandText =
                @"
                    SELECT id, title, year, timestamp
                    FROM book WHERE title LIKE $title
                ";
                command.Parameters.AddWithValue("$title", "%" + title + "%");

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        books.Add(new Book(reader.GetInt32(0), reader.GetString(1), 1111));
                    }
                }
            }

            return books;
        }

        public static ObservableCollection<Book> ReadDataBase2(List<Tuple<string, string>> parameters, int firstRow, int rowsCount, ref int totalRowsCount)
        {
            ObservableCollection<Book> books = new ObservableCollection<Book>();

            if (!File.Exists(dbName)) return books;

            using (SqliteConnection connection = new SqliteConnection("Data Source=" + dbName))
            {
                connection.Open();
                totalRowsCount = readAllRows(connection);

                SqliteCommand command = connection.CreateCommand();

                string selectSyntax = "SELECT id, title, year, timestamp FROM book";
                command.CommandText = selectSyntax;
                if (parameters == null)
                {
                    command.CommandText = selectSyntax + " LIMIT $firstRow, $rowsCount";
                    return books;
                }

                List<Tuple<string, string>> valueParameters = new List<Tuple<string, string>>();
                valueParameters = prepareSqlQuery(parameters, firstRow, rowsCount);
                command.CommandText = valueParameters[valueParameters.Count - 1].Item2;

                //valueParameters.TrimExcess();
                for (int i = 1; i < valueParameters.Count - 1; i++)
                {
                    command.Parameters.AddWithValue("$" + valueParameters[i].Item1, valueParameters[i].Item2);
                }
                //command.Parameters.AddWithValue("$title", "%visual%");
                command.Parameters.AddWithValue("$firstRow", firstRow);
                command.Parameters.AddWithValue("$rowsCount", rowsCount);

                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        books.Add(new Book(reader.GetInt32(0), reader.GetString(1), 1111));
                    }
                }
            }

            return books;
        }

        public static ObservableCollection<Book> ReadDataBase(List<Tuple<string, string>> parameters, int firstRow, int rowsCount, ref int totalRowsCount)
        {
            ObservableCollection<Book> books = new ObservableCollection<Book>();

            if (!File.Exists(dbName)) return books;
            //if (bookParameters == null) return books;

            using (SqliteConnection connection = new SqliteConnection("Data Source=" + dbName))
            {
                //SqliteConnection connection = new SqliteConnection("Data Source=" + dbName);
                connection.Open();
                totalRowsCount = readAllRows(connection);

                SqliteCommand command = connection.CreateCommand();
                //command = createSelectSyntax(parameters, command, firstRow, rowsCount);
                //createSelectSyntax(parameters, command, firstRow, rowsCount);

                command.CommandText = "SELECT id, title, year, timestamp FROM book WHERE title LIKE $title LIMIT $firstRow, $rowsCount";
                command.Parameters.AddWithValue("$firstRow", firstRow);
                command.Parameters.AddWithValue("$rowsCount", rowsCount);
                command.Parameters.AddWithValue("$title", "%visual%");

                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        books.Add(new Book(reader.GetInt32(0), reader.GetString(1), 1111));
                    }
                }
            }

            return books;
        }

        private static List<Tuple<string, string>> prepareSqlQuery(List<Tuple<string, string>> parameters, int firstRow, int rowsCount)
        {
            string selectSyntax = "SELECT id, title, year, timestamp FROM book";
            if (parameters == null)
            {
                selectSyntax += " LIMIT $firstRow, $rowsCount";
                parameters.Add(new Tuple<string, string>("Query", selectSyntax));
                return parameters;
            }

            List<int> removeIndexList = new List<int>();
            for (int i = 1; i < parameters.Count; i++)
            {
                if (parameters[i].Item2 == "0" || parameters[i].Item2 == "01.01.0001 00:00:00" || parameters[i].Item2 == string.Empty || parameters[i].Item2 == null)
                {
                    removeIndexList.Add(i);
                }
            }
            for (int i = removeIndexList.Count - 1; i >= 0; i--) parameters.RemoveAt(removeIndexList[i]);
            //parameters.TrimExcess();
            if (parameters.Count == 0)
            {
                selectSyntax += " LIMIT $firstRow, $rowsCount";
                parameters.Add(new Tuple<string, string>("Query", selectSyntax));
                return parameters;
            }

            bool andFlag = false;
            selectSyntax += " WHERE";
            if (parameters[0].Item2 == "Author") throw new NotImplementedException();
            if (parameters[0].Item2 == "Book")
            {
                for (int i = 0; i < parameters.Count; i++)
                {
                    switch (parameters[i].Item1)
                    {
                        case "ID":
                            if (andFlag) selectSyntax += " AND";
                            selectSyntax += " id = $id";
                            //parameters.RemoveAt(i);
                            parameters[i] = new Tuple<string, string>("id", parameters[i].Item2);
                            andFlag = true;
                            break;

                        case "Title":
                            if (andFlag) selectSyntax += " AND";
                            selectSyntax += " title LIKE $title";
                            //parameters.RemoveAt(i);
                            parameters[i] = new Tuple<string, string>("title", "%" + parameters[i].Item2 + "%");
                            andFlag = true;
                            break;

                        case "Year":
                            if (andFlag) selectSyntax += " AND";
                            selectSyntax += " year = $year";
                            andFlag = true;
                            break;
                    }
                }
            }
            if (parameters[0].Item2 == "Reader") throw new NotImplementedException();

            parameters.Add(new Tuple<string, string>("Query", selectSyntax));

            return parameters;
        }

        private static int readAllRows(SqliteConnection connection)
        {
            if (connection == null) return -1;

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = @"SELECT COUNT(*) FROM book;";
            using (SqliteDataReader reader = command.ExecuteReader())
            {
                reader.Read();
                return reader.GetInt32(0);
            }
        }
    }
}
