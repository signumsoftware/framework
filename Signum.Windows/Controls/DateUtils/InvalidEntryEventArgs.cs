//---------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All rights reserved.
//
//---------------------------------------------------------------------------
using System;
using System.Windows;

namespace Signum.Windows.DateUtils
{
    /// <summary>
    /// The InvalidEntry event args, occurs when the datepicker can't parse user input string correctly
    /// </summary>
    public class InvalidEntryEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public InvalidEntryEventArgs(RoutedEvent id, string entry)
            : base()
        {
            if (id == null)
            {
                throw new ArgumentNullException("id");
            }

            RoutedEvent = id;
            _entry = entry;
        }

        /// <summary>
        /// The input string
        /// </summary>
        public string Entry
        {
            get { return _entry; }
        }

        /// <summary>
        /// This method is used to perform the proper type casting in order to
        /// call the type-safe InvalidEntryEventHandler delegate for the InvalidEntry event.
        /// </summary>
        /// <param name="genericHandler">The handler to invoke.</param>
        /// <param name="genericTarget">The current object along the event's route.</param>
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            InvalidEntryEventHandler handler = (InvalidEntryEventHandler)genericHandler;
            handler(genericTarget, this);
        }

        private string _entry;
    }

    /// <summary>
    /// The delegate type for handling the InvalidEntry event
    /// </summary>
    public delegate void InvalidEntryEventHandler(object sender, InvalidEntryEventArgs e);

}
