using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Operations;
using Signum.Engine;
using Signum.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Utilities;

namespace Signum.Test.Extensions
{
    public class AlbumGraph : Graph<AlbumDN, AlbumState>
    {
        public static void Register()
        {
            GetState = f => (f.IsNew) ? AlbumState.New : AlbumState.Saved;

            new Execute(AlbumOperation.Save)
            {
                FromStates = new[] { AlbumState.New },
                ToState = AlbumState.Saved,
                AllowsNew = true,
                Lite = false,
                Execute = (album, _) => { album.Save(); },
            }.Register();

            new Execute(AlbumOperation.Modify)
            {
                FromStates = new[] { AlbumState.Saved },
                ToState = AlbumState.Saved,
                AllowsNew = false,
                Lite = false,
                Execute = (album, _) => { },
            }.Register();

            new ConstructFrom<BandDN>(AlbumOperation.CreateFromBand)
            {
                ToState = AlbumState.Saved,
                AllowsNew = false,
                Lite = true,
                Construct = (BandDN band, object[] args) =>
                    new AlbumDN
                    {
                        Author = band,
                        Name = args.GetArg<string>(),
                        Year = args.GetArg<int>(),
                        Label = args.GetArg<LabelDN>()
                    }.Save()
            }.Register();

            new ConstructFrom<AlbumDN>(AlbumOperation.Clone)
            {
                ToState = AlbumState.New,
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
            }.Register();

            new ConstructFromMany<AlbumDN>(AlbumOperation.CreateGreatestHitsAlbum)
            {
                ToState = AlbumState.New,
                Construct = (albumLites, _) =>
                {
                    List<AlbumDN> albums = albumLites.Select(a => a.Retrieve()).ToList();
                    if (albums.Select(a => a.Author).Distinct().Count() > 1)
                        throw new ArgumentException("All album authors must be the same in order to create a Greatest Hits Album");

                    return new AlbumDN()
                    {
                        Author = albums.FirstEx().Author,
                        Year = DateTime.Now.Year,
                        Songs = albums.SelectMany(a => a.Songs).ToMList()
                    };
                }
            }.Register();


            new ConstructFromMany<AlbumDN>(AlbumOperation.CreateEmptyGreatestHitsAlbum)
            {
                ToState = AlbumState.New,
                Construct = (albumLites, _) =>
                {
                    List<AlbumDN> albums = albumLites.Select(a => a.Retrieve()).ToList();
                    if (albums.Select(a => a.Author).Distinct().Count() > 1)
                        throw new ArgumentException("All album authors must be the same in order to create a Greatest Hits Album");

                    return new AlbumDN()
                    {
                        Author = albums.FirstEx().Author,
                        Year = DateTime.Now.Year,
                    };
                }
            }.Register();
        }
    }
}
