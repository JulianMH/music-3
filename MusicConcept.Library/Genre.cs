using SQLite;
using System;
namespace MusicConcept.Library
{
    public class Genre
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }

        public Genre(string name)
        {
            this.Name = name;
        }

        public Genre()
        {

        }
    }
}
