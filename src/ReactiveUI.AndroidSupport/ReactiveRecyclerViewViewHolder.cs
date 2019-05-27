// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Runtime.Serialization;
using Android.Support.V7.Widget;
using Android.Views;

namespace ReactiveUI.AndroidSupport
{
    /// <summary>
    /// A <see cref="RecyclerView.ViewHolder"/> implementation that binds to a reactive view model.
    /// </summary>
    /// <typeparam name="TViewModel">The type of the view model.</typeparam>
    public class ReactiveRecyclerViewViewHolder<TViewModel> : RecyclerView.ViewHolder, ILayoutViewHost, IViewFor<TViewModel>, IReactiveNotifyPropertyChanged<ReactiveRecyclerViewViewHolder<TViewModel>>, IReactiveObject, ICanActivate
            where TViewModel : class, IReactiveObject
    {
        /// <summary>
        /// Disposes a disposable resources that are disposed together when the View Holder is Disposed.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401: Field should be private", Justification = "Legacy reasons")]
        protected readonly CompositeDisposable CompositeDisposable = new CompositeDisposable();

        /// <summary>
        /// Gets all public accessible properties.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401: Field should be private", Justification = "Legacy reasons")]
        [SuppressMessage("Design", "CA1051: Do not declare visible instance fields", Justification = "Legacy reasons")]
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1306: Field should start with a lower case letter", Justification = "Legacy reasons")]
        [IgnoreDataMember]
        protected Lazy<PropertyInfo[]> AllPublicProperties;

        private readonly Subject<Unit> _activated = new Subject<Unit>();
        private readonly Subject<Unit> _deactivated = new Subject<Unit>();

        private TViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReactiveRecyclerViewViewHolder{TViewModel}"/> class.
        /// </summary>
        /// <param name="view">The view.</param>
        protected ReactiveRecyclerViewViewHolder(View view)
            : base(view)
        {
            CompositeDisposable.Add(_activated);
            CompositeDisposable.Add(_deactivated);
            SetupRxObj();

            Selected = Observable.FromEventPattern(
                h => view.Click += h,
                h => view.Click -= h)
                    .Select(_ => AdapterPosition);

            LongClicked = Observable.FromEventPattern<View.LongClickEventArgs>(
                h => view.LongClick += h,
                h => view.LongClick -= h)
                    .Select(_ => AdapterPosition);
        }

        /// <inheritdoc/>
        public event PropertyChangingEventHandler PropertyChanging
        {
            add => PropertyChangingEventManager.AddHandler(this, value);
            remove => PropertyChangingEventManager.RemoveHandler(this, value);
        }

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged
        {
            add => PropertyChangedEventManager.AddHandler(this, value);
            remove => PropertyChangedEventManager.RemoveHandler(this, value);
        }

        /// <summary>
        /// Gets an observable that signals that this ViewHolder has been selected.
        ///
        /// The <see cref="int"/> is the position of this ViewHolder in the <see cref="RecyclerView"/>
        /// and corresponds to the <see cref="RecyclerView.ViewHolder.AdapterPosition"/> property.
        /// </summary>
        public IObservable<int> Selected { get; }

        /// <summary>
        /// Gets an observable that signals that this ViewHolder has been long-clicked.
        ///
        /// The <see cref="int"/> is the position of this ViewHolder in the <see cref="RecyclerView"/>
        /// and corresponds to the <see cref="RecyclerView.ViewHolder.AdapterPosition"/> property.
        /// </summary>
        public IObservable<int> LongClicked { get; }

        /// <summary>
        /// Gets the current view being shown.
        /// </summary>
        public View View => ItemView;

        /// <inheritdoc/>
        public TViewModel ViewModel
        {
            get => _viewModel;
            set
            {
                var willActivate = value == null;
                this.RaiseAndSetIfChanged(ref _viewModel, value);
                if (willActivate)
                {
                    _activated.OnNext(Unit.Default);
                }
            }
        }

        /// <summary>
        /// Gets a signal when the ViewModel is attached for the first time.
        /// </summary>
        public IObservable<Unit> Activated => _activated.AsObservable();

        /// <summary>
        /// Gets a signal when the viewholder is deactivated.
        /// </summary>
        public IObservable<Unit> Deactivated => _deactivated.AsObservable();

        /// <summary>
        /// Gets an observable which signals when exceptions are thrown.
        /// </summary>
        [IgnoreDataMember]
        public IObservable<Exception> ThrownExceptions => this.GetThrownExceptionsObservable();

        /// <inheritdoc/>
        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (TViewModel)value;
        }

        /// <inheritdoc/>
        [IgnoreDataMember]
        public IObservable<IReactivePropertyChangedEventArgs<ReactiveRecyclerViewViewHolder<TViewModel>>> Changing => this.GetChangingObservable();

        /// <inheritdoc/>
        [IgnoreDataMember]
        public IObservable<IReactivePropertyChangedEventArgs<ReactiveRecyclerViewViewHolder<TViewModel>>> Changed => this.GetChangedObservable();

        /// <inheritdoc/>
        public IDisposable SuppressChangeNotifications()
        {
            return IReactiveObjectExtensions.SuppressChangeNotifications(this);
        }

        /// <summary>
        /// Gets if change notifications via the INotifyPropertyChanged interface are being sent.
        /// </summary>
        /// <returns>A value indicating whether change notifications are enabled or not.</returns>
        public bool AreChangeNotificationsEnabled()
        {
            return IReactiveObjectExtensions.AreChangeNotificationsEnabled(this);
        }

        /// <inheritdoc/>
        void IReactiveObject.RaisePropertyChanging(PropertyChangingEventArgs args)
        {
            PropertyChangingEventManager.DeliverEvent(this, args);
        }

        /// <inheritdoc/>
        void IReactiveObject.RaisePropertyChanged(PropertyChangedEventArgs args)
        {
            PropertyChangedEventManager.DeliverEvent(this, args);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CompositeDisposable?.Dispose();
            }

            base.Dispose(disposing);
        }

        [OnDeserialized]
        private void SetupRxObj(StreamingContext sc)
        {
            SetupRxObj();
        }

        private void SetupRxObj()
        {
            AllPublicProperties = new Lazy<PropertyInfo[]>(() =>
                GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).ToArray());
        }
    }
}
