﻿using System;
using System.Linq;
using Xunit;
using Signum.Engine;
using Signum.Entities;
using Signum.Utilities;
using Signum.Test.Environment;

namespace Signum.Test.LinqProvider
{
    /// <summary>
    /// Summary description for LinqProvider
    /// </summary>
    public class SelectSortCircuitTest
    {
        public SelectSortCircuitTest()
        {
            MusicStarter.StartAndLoad();
            Connector.CurrentLogger = new DebugTextWriter();
        }

        [Fact]
        public void SortCircuitCoalesce()
        {
            var list = Database.Query<AlbumEntity>().Where(a => ("Hola") == null).Select(a => a.Year).ToList();
        }

        [Fact]
        public void SortCircuitCoalesceNullable()
        {
            var list = Database.Query<AlbumEntity>().Where(a => (((DateTime?)DateTime.Now) ?? Throw<DateTime>()) == DateTime.Today).Select(a => a.Year).ToList();
        }


        [Fact]
        public void SortCircuitConditionalIf()
        {
            var list = Database.Query<AlbumEntity>().Where(a => true).Select(a => a.Year).ToList();
        }

        [Fact]
        public void NonSortCircuitCondicional()
        {
            var list = Database.Query<BandEntity>().Where(b => b.Name == "Olmo" ? b.Members.Any(a => a.Name == "A") : true).Select(b => b.ToLite()).ToList();

        }

        [Fact]
        public void SortCircuitOr()
        {
            var list = Database.Query<AlbumEntity>().Where(a => true | Throw<bool>()).Select(a => a.Year).ToList();
        }

        [Fact]
        public void SortCircuitOrElse()
        {
            var list = Database.Query<AlbumEntity>().Where(a => true ).Select(a => a.Year).ToList();
        }

        [Fact]
        public void SortCircuitAnd()
        {
            var list = Database.Query<AlbumEntity>().Where(a => false & Throw<bool>()).Select(a => a.Year).ToList();
        }

        [Fact]
        public void SortCircuitAndAlso()
        {
            var list = Database.Query<AlbumEntity>().Where(a => false).Select(a => a.Year).ToList();
        }

        [Fact]
        public void SortEqualsTrue()
        {
            var list = Database.Query<AlbumEntity>().Where(a => true == (a.Year == 1900)).Select(a => a.Year).ToList();
        }

        [Fact]
        public void SortEqualsFalse()
        {
            var list = Database.Query<AlbumEntity>().Where(a => false == (a.Year == 1900)).Select(a => a.Year).ToList();
        }

        public T Throw<T>()
        {
            throw new InvalidOperationException("This {0} should not be evaluated".FormatWith(typeof(T).Name));
        }
    }
}
