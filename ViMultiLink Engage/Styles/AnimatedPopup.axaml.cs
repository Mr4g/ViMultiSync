using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;



namespace ViMultiSync
{
    public partial class AnimatedPopup : ContentControl
    {
        public event EventHandler Opened;
        public event EventHandler Closed;

        #region Private Members

        /// <summary>
        /// The underlay control for closing this popup
        /// </summary>
        private Control mUnderlayControl;

        /// <summary>
        /// Indicates if we have captured the opacity value yet
        /// </summary>
        private bool mOpacityCaptured = false;

        /// <summary>
        /// Indicates if this is the first time we are animating 
        /// </summary>
        private bool mFirstAnimation = true;

        /// <summary>
        /// Store the controls original Opacity value at startup
        /// </summary>
        private double mOrginalOpacity;

        // Get a 60 FPS timespan
        private TimeSpan mFrameRate = TimeSpan.FromSeconds(1 / 60.0);

        // Calculate total tick that make up the animation time
        private int mTotalTicks => (int)(_animationTime.TotalSeconds / mFrameRate.TotalSeconds);

        /// <summary>
        /// Store the controls desired size
        /// </summary>

        private Size mDesiredSize;

        /// <summary>
        /// A flag for when we are animating
        /// </summary>
        /// 
        private bool mAnimating;

        /// <summary>
        ///  Keeps track of if we have found the desired 100% width/height auto size
        /// </summary>
        private bool mSizeFound;

        /// <summary>
        /// The animation UI timer
        /// </summary>
        private DispatcherTimer mAnimationTimer;

        /// <summary>
        /// The timeout timer to detect when auto-sizing has finished firing
        /// </summary>
        private Timer mSizingTimer;

        /// <summary>
        /// The current position in the animation 
        /// </summary>
        private int mAnimationCurrentTick;

        #endregion

        #region Public Propertis

        private bool _isMinimized;

        public static readonly DirectProperty<AnimatedPopup, bool> IsMinimizedProperty =
            AvaloniaProperty.RegisterDirect<AnimatedPopup, bool>(
                nameof(IsMinimized),
                o => o.IsMinimized,
                (o, v) => o.IsMinimized = v);

        public bool IsMinimized
        {
            get => _isMinimized;
            set => SetAndRaise(IsMinimizedProperty, ref _isMinimized, value);
        }

        #region Minimized Width

        private double _minimizedWidth = 100; // Domyœlna wartoœæ, mo¿esz dostosowaæ do w³asnych potrzeb

        public static readonly DirectProperty<AnimatedPopup, double> MinimizedWidthProperty =
            AvaloniaProperty.RegisterDirect<AnimatedPopup, double>(
                nameof(MinimizedWidth),
                o => o.MinimizedWidth,
                (o, v) => o.MinimizedWidth = v);

        public double MinimizedWidth
        {
            get => _minimizedWidth;
            set => SetAndRaise(MinimizedWidthProperty, ref _minimizedWidth, value);
        }

        #endregion

        #region Minimized Height

        private double _minimizedHeight = 50; // Domyœlna wartoœæ, mo¿esz dostosowaæ do w³asnych potrzeb

        public static readonly DirectProperty<AnimatedPopup, double> MinimizedHeightProperty =
            AvaloniaProperty.RegisterDirect<AnimatedPopup, double>(
                nameof(MinimizedHeight),
                o => o.MinimizedHeight,
                (o, v) => o.MinimizedHeight = v);

        public double MinimizedHeight
        {
            get => _minimizedHeight;
            set => SetAndRaise(MinimizedHeightProperty, ref _minimizedHeight, value);
        }

        #endregion


        /// <summary>
        /// Indicates if the control is currently opened
        /// </summary>
        public bool IsOpened => mAnimationCurrentTick >= mTotalTicks;

        #region Open

        private bool _open;

        public static readonly DirectProperty<AnimatedPopup, bool> OpenProperty = AvaloniaProperty.RegisterDirect<AnimatedPopup, bool>(
            nameof(Open),
            o => o.Open,
            (o, v) => o.Open = v);

        private bool _duringOpening;

        public static readonly DirectProperty<AnimatedPopup, bool> DuringOpeningProperty = AvaloniaProperty.RegisterDirect<AnimatedPopup, bool>(
            nameof(DuringOpening),
            o => o.DuringOpening,
            (o, v) => o.DuringOpening = v);

        /// <summary>
        ///  Property to set whether the control should be open or closed 
        /// </summary>

        public bool Open
        {
            get => _open;
            set
            {
                // If we value has not changed
                if (value == _open)
                    // Do nothing
                    return;

                // If we are opening...
                if (value)
                {
                    // If the parent is a grid...
                    if (Parent is Grid grid)
                    {
                        Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            // Set grid row/column span
                            if (grid.RowDefinitions?.Count > 0)
                                mUnderlayControl.SetValue(Grid.RowSpanProperty, grid.RowDefinitions.Count);

                            if (grid.ColumnDefinitions?.Count > 0)
                                mUnderlayControl.SetValue(Grid.ColumnSpanProperty, grid.ColumnDefinitions.Count);

                            // Inject the underlay control
                            if (!grid.Children.Contains(mUnderlayControl))
                                grid.Children.Insert(0, mUnderlayControl);
                        });

                    }
                }

                // If closing..
                else
                {
                    // If the control is currently fully open...
                    if (IsOpened)
                        // Update desired size
                        UpdateDesiredSize();
                }

                // Update animation
                UpdateAnimation();

                // Raise the property changed event
                SetAndRaise(OpenProperty, ref _open, value);
            }
        }

        public bool DuringOpening
        {
            get => _duringOpening;
            set => SetAndRaise(DuringOpeningProperty, ref _duringOpening, value);

        }

        #endregion

        #region Animation Time
        private TimeSpan _animationTime = TimeSpan.FromSeconds(3);

        public static readonly DirectProperty<AnimatedPopup, TimeSpan> AnimationTimeDirectProperty = AvaloniaProperty.RegisterDirect<AnimatedPopup, TimeSpan>(
            nameof(AnimationTime),
            o => o.AnimationTime,
            (o, v) => o.AnimationTime = v);

        /// <summary>
        /// Property to set whether the control should be open or closed
        /// </summary>

        public TimeSpan AnimationTime
        {
            get => _animationTime;
            set => SetAndRaise(AnimationTimeDirectProperty, ref _animationTime, value);
        }



        #endregion

        #region Animate Opacity

        private bool _animateOpacity = true;

        public static readonly DirectProperty<AnimatedPopup, bool> AnimateOpacityProperty = AvaloniaProperty.RegisterDirect<AnimatedPopup, bool>(
            nameof(AnimateOpacity),
            o => o.AnimateOpacity,
            (o, v) => o.AnimateOpacity = v);

        public bool AnimateOpacity
        {
            get => _animateOpacity;
            set => SetAndRaise(AnimateOpacityProperty, ref _animateOpacity, value);
        }

        #endregion

        #region Underlay Opacity

        private double _underlayOpacity = 0.2;

        public static readonly DirectProperty<AnimatedPopup, double> UnderlayOpacityProperty = AvaloniaProperty.RegisterDirect<AnimatedPopup, double>(
            nameof(UnderlayOpacity),
            o => o.UnderlayOpacity,
            (o, v) => o.UnderlayOpacity = v);

        public double UnderlayOpacity
        {
            get => _underlayOpacity;
            set => SetAndRaise(UnderlayOpacityProperty, ref _underlayOpacity, value);
        }

        #endregion

        #endregion

        #region Public Commands

        [RelayCommand]
        public void BeginOpen()
        {
            DuringOpening = true;
            Open = true;

        }

        [RelayCommand]
        public void BeginClose()
        {
            Open = false;
            DuringOpening = false;
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Default Constructor
        /// </summary>
        public AnimatedPopup()
        {

            // Make a new underlay control
            mUnderlayControl = new Border
            {
                Background = Brushes.Black,
                Opacity = 0,
                ZIndex = 9
            };

            // On press, close popup
            mUnderlayControl.PointerPressed += (sender, args) =>
            {
                BeginClose();
               // OnClosed();
            };


            // Make a new dispatch timer
            mAnimationTimer = new DispatcherTimer
            {
                Interval = mFrameRate
            };

            mSizingTimer = new Timer(t =>
            {
                // If we have already calculated the size...
                if (mSizeFound)
                    // no longer accept new sizes 
                    return;

                // We have now found our desired size
                mSizeFound = true;

                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    // Update the desired size
                    UpdateDesiredSize();

                    // Update animation
                    UpdateAnimation();
                });
            });

            // Callback on every tick
            mAnimationTimer.Tick += (s, e) => AnimationTick();
        }

        #endregion

        #region Private Methods

        private void OnOpened()
        {
            Opened?.Invoke(this, EventArgs.Empty);
        }

        private void OnClosed()
        {
            Closed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Calculate and start any new required animations
        /// </summary>

        /// <summary>
        /// Updates the animation desired size based on the current visuals desired size
        /// </summary>
        private void UpdateDesiredSize()
        {
            // Przypisz wartoœæ do mDesiredSize
            mDesiredSize = DesiredSize - Margin;
        }

        private void UpdateAnimation()
        {
            // Do nothing if we still haven't found our initial size
            if (!mSizeFound)
                return;

            // Start the animation thread again 
            mAnimationTimer.Start();
        }

        /// <summary>
        /// Should be called when an open or close transition has complete
        /// </summary>
        private void AnimationComplete()
        {

            // If open...
            if (_open)
            {
                // Set size to desired size
                Width = double.NaN;
                Height = double.NaN;

                // Make sure opacity is set to original value
                Opacity = mOrginalOpacity;
            }
            // If close
            else
            {
                if (IsMinimized)
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Width = MinimizedWidth;
                        Height = MinimizedHeight;
                    });
                }
                else
                {
                    // Set size to 0...
                    Width = 0;
                    Height = 0;
                }

                // If the parent is a grid...
                if (Parent is Grid grid)
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        // Reset opacity 
                        mUnderlayControl.Opacity = 0;

                        // Remove the underlay 
                        if (grid.Children.Contains(mUnderlayControl))
                            grid.Children.Remove(mUnderlayControl);
                    });

                }
            }

            if (Open)
            {
                OnOpened();
            }
            else
            {
                OnClosed();
            }

        }

        /// <summary>
        /// Update controls sizes based on the next tick of an animation
        /// </summary>
        private void AnimationTick()
        {
            // If this the first call after calculating the desired size..
            if (mFirstAnimation)
            {
                Width = _open ? mDesiredSize.Width : 0;
                Height = _open ? mDesiredSize.Height : 0;

                // Clear the flag 
                mFirstAnimation = false;

                // Stop this animation timer
                mAnimationTimer.Stop();

                // Reset opacity
                Opacity = mOrginalOpacity;

                // Set the final size 
                AnimationComplete();

                // Do on this tick
                return;
            }

            // If we have reached the end of our animation...
            if ((_open && mAnimationCurrentTick >= mTotalTicks) || (!_open && mAnimationCurrentTick == 00))
            {
                // Stop this animation timer
                mAnimationTimer.Stop();

                // Set the final size 
                AnimationComplete();

                //Clear Animating flag
                mAnimating = false;

                // Break out of code
                return;
            }

            //Set animating flag
            mAnimating = true;

            // Move the tick in the right direction
            mAnimationCurrentTick += _open ? 1 : -1;


            // Get percentage of the way through the current animation
            var percentageAnimated = (float)mAnimationCurrentTick / mTotalTicks;

            // Make ab animation easing
            var easing = new QuadraticEaseIn();

            // Calculate final width and height

            var finalWidth = mDesiredSize.Width * easing.Ease(percentageAnimated);
            var finalHeight = !IsMinimized ? mDesiredSize.Height * easing.Ease(percentageAnimated) : MinimizedHeight;


            // Do animation

            Width = finalWidth;
            Height = finalHeight;

            // Animate opacity
            if (!IsMinimized && AnimateOpacity)
                Opacity = mOrginalOpacity * easing.Ease(percentageAnimated);

            // Animate underlay
            mUnderlayControl.Opacity = _underlayOpacity * easing.Ease(percentageAnimated);

            Debug.WriteLine($"Current tick: {mAnimationCurrentTick}");
        }


        #endregion
        public override void Render(DrawingContext context)
        {
            // If we have not
            if (!mSizeFound)
            {
                // IF we have not yet captured the opacity 
                if (!mOpacityCaptured)
                {

                    // Set flag to true
                    mOpacityCaptured = true;

                    // Remember original controls opacity
                    mOrginalOpacity = Opacity;

                    // Hide control
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Opacity = 0;
                    });
                }

                mSizingTimer.Change(1, int.MaxValue);
            }

            base.Render(context);
        }
    }
}
