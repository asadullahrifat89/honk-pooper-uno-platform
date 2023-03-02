﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Runtime.CompilerServices;
using Windows.Foundation;

namespace HonkPooper
{
    public partial class Construct : Border
    {
        #region Fields

        private readonly CompositeTransform _compositeTransform = new()
        {
            CenterX = 0.5,
            CenterY = 0.5,
            Rotation = 0,
            ScaleX = 1,
            ScaleY = 1,
        };

        #endregion

        #region Properties

        public double Speed { get; set; }

        public ConstructType ConstructType { get; set; }

        private Func<Construct, bool> AnimateAction { get; set; }

        private Func<Construct, bool> RecycleAction { get; set; }

        #endregion

        #region Ctor

        public Construct()
        {
            RenderTransformOrigin = new Point(0.5, 0.5);

            RenderTransform = _compositeTransform;
            CanDrag = false;
        }

        public Construct(
            double speed,
            ConstructType constructType,
            double width,
            double height,
            Func<Construct, bool> animateAction,
            Func<Construct, bool> recycleAction,
            UIElement content = null)
        {
            Speed = speed;
            ConstructType = constructType;
            AnimateAction = animateAction;
            RecycleAction = recycleAction;

            RenderTransformOrigin = new Point(0.5, 0.5);

            RenderTransform = _compositeTransform;
            CanDrag = false;

            SetSize(width: width, height: height);

            if (content is not null)
                SetChild(content);
        }

        #endregion

        #region Methods     

        public void SetAction(
            Func<Construct, bool> movementAction,
            Func<Construct, bool> recycleAction)
        {
            AnimateAction = movementAction;
            RecycleAction = recycleAction;
        }

        public void Animate()
        {
            AnimateAction(this);
        }

        public void Recycle()
        {
            RecycleAction(this);
        }

        public void SetSize(double width, double height)
        {
            Width = width;
            Height = height;
        }

        public double GetTop()
        {
            return Canvas.GetTop(this);
        }

        public double GetBottom()
        {
            return Canvas.GetTop(this) + Height;
        }

        public double GetLeft()
        {
            return Canvas.GetLeft(this);
        }

        public double GetRight()
        {
            return Canvas.GetLeft(this) + Width;
        }

        public int GetZ()
        {
            return Canvas.GetZIndex(this);
        }

        public void SetTop(double top)
        {
            Canvas.SetTop(this, top);
        }

        public void SetLeft(double left)
        {
            Canvas.SetLeft(this, left);
        }

        public void SetZ(int z)
        {
            Canvas.SetZIndex(this, z);
        }

        public void SetPosition(double left, double top)
        {
            Canvas.SetTop(this, top);
            Canvas.SetLeft(this, left);
        }

        public void SetScaleTransform(double scaleXY)
        {
            _compositeTransform.ScaleX = scaleXY;
            _compositeTransform.ScaleY = scaleXY;
        }

        public void SetScaleTransform(double scaleX, double scaleY)
        {
            _compositeTransform.ScaleX = scaleX;
            _compositeTransform.ScaleY = scaleY;
        }

        public double GetScaleX()
        {
            return _compositeTransform.ScaleX;
        }

        public double GetScaleY()
        {
            return _compositeTransform.ScaleY;
        }

        public void SetScaleX(double scaleX)
        {
            _compositeTransform.ScaleX = scaleX;
        }

        public void SetScaleY(double scaleY)
        {
            _compositeTransform.ScaleY = scaleY;
        }

        public void SetRotation(double rotation)
        {
            _compositeTransform.Rotation = rotation;
        }

        public void SetSkewX(double skewX)
        {
            _compositeTransform.SkewX = skewX;
        }

        public void SetSkewY(double skewY)
        {
            _compositeTransform.SkewY = skewY;
        }

        public void SetChild(UIElement uIElement)
        {
            Child = uIElement;
        }

        #endregion
    }
}
