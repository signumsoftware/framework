using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Operations;
using Signum.Engine;
using Signum.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Signum.Test.Extensions
{
    public enum AlbumState
    {
        New,
        Saved
    }

    public enum AlbumOperation
    {
        Save,
        Modify,
        CreateFromBand,
        Delete,
        Clone,
        CreateGreatestHitsAlbum,
        CreateEmptyGreatestHitsAlbum
    }

    public class AlbumGraph : Graph<AlbumDN, AlbumState>
    {
        public AlbumGraph()
        {
            this.GetState = f => (f.IsNew) ? AlbumState.New : AlbumState.Saved;

            this.Operations = new List<IGraphOperation>
            { 
                new Goto(AlbumOperation.Save, AlbumState.Saved)
                { 
                    AllowsNew = true,
                    Lite = false,
                    FromStates = new [] { AlbumState.New },
                    Returns = true,
                    Execute = (album, _) => { album.Save(); },
                },

                new Goto(AlbumOperation.Modify, AlbumState.Saved)
                { 
                    AllowsNew = false,
                    FromStates = new [] { AlbumState.Saved },
                    Lite = false,
                    Returns = true,
                    Execute = (album, _) => {},
                },

                new ConstructFrom<BandDN>(AlbumOperation.CreateFromBand, AlbumState.Saved)
                {
                    AllowsNew = false,
                    Lite = true,
                    Construct = (BandDN band, object[] args) => 
                        new AlbumDN
                        {
                            Author = band,
                            Name = args.GetArg<string>(0),
                            Year = args.GetArg<int>(1),
                            Label = args.GetArg<LabelDN>(2)
                        }.Save()
                },

                new Delete(AlbumOperation.Delete)
                {
                    Delete = (album, _) => album.Delete()
                },

                new ConstructFrom<AlbumDN>(AlbumOperation.Clone, AlbumState.New)
                {
                    AllowsNew = false,
                    Lite = true,
                    Construct = (g, args) => 
                    { 
                        return new AlbumDN
                        {
                            Author = g.Author,
                            Label = g.Label,
                        };
                    }
                },

                new ConstructFromMany<AlbumDN>(AlbumOperation.CreateGreatestHitsAlbum, AlbumState.New)
                {
                    Constructor = (albumLites, _) => 
                    {
                        List<AlbumDN> albums = albumLites.Select(a => a.Retrieve()).ToList();
                        if (albums.Select(a => a.Author).Distinct().Count() > 1)
                            throw new ArgumentException("All album authors must be the same in order to create a Greatest Hits Album");

                        return new AlbumDN()
                        {
                            Author = albums.First().Author,
                            Year = DateTime.Now.Year,
                            Songs = albums.SelectMany(a => a.Songs).ToMList()
                        };
                    }
                },

                
                new ConstructFromMany<AlbumDN>(AlbumOperation.CreateEmptyGreatestHitsAlbum, AlbumState.New)
                {
                    Constructor = (albumLites, _) => 
                    {
                        List<AlbumDN> albums = albumLites.Select(a => a.Retrieve()).ToList();
                        if (albums.Select(a => a.Author).Distinct().Count() > 1)
                            throw new ArgumentException("All album authors must be the same in order to create a Greatest Hits Album");

                        return new AlbumDN()
                        {
                            Author = albums.First().Author,
                            Year = DateTime.Now.Year,
                        };
                    }
                },                
            };
        }
    }
}
