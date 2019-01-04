﻿using System;
using System.Collections.Generic;
using Unity.UIWidgets.animation;
using Unity.UIWidgets.foundation;
using Unity.UIWidgets.gestures;
using Unity.UIWidgets.painting;
using Unity.UIWidgets.ui;
using UnityEngine;
using Canvas = Unity.UIWidgets.ui.Canvas;
using Rect = Unity.UIWidgets.ui.Rect;
using Color = Unity.UIWidgets.ui.Color;

namespace Unity.UIWidgets.rendering {
    public interface RenderAbstractViewport {
        RevealedOffset getOffsetToReveal(RenderObject target, double alignment, Rect rect = null);
        RenderObject parent { get; }
    }

    public static class RenderViewportUtils {
        public static RenderAbstractViewport of(RenderObject obj) {
            while (obj != null) {
                if (obj is RenderAbstractViewport) {
                    return (RenderAbstractViewport) obj;
                }

                obj = (RenderObject) obj.parent;
            }

            return null;
        }

        public const double defaultCacheExtent = 250.0;
    }

    public class RevealedOffset {
        public RevealedOffset(
            double offset,
            Rect rect) {
            D.assert(rect != null);
            this.offset = offset;
            this.rect = rect;
        }

        public readonly double offset;
        public readonly Rect rect;

        public override string ToString() {
            return string.Format("{0}(offset: {1}, rect: {2})", this.GetType(), this.offset, this.rect);
        }
    }

    public abstract class RenderViewportBase<ParentDataClass> :
        ContainerRenderObjectMixinRenderBox<RenderSliver, ParentDataClass>,
        RenderAbstractViewport
        where ParentDataClass : ParentData, ContainerParentDataMixin<RenderSliver> {
        protected RenderViewportBase(
            AxisDirection axisDirection = AxisDirection.down,
            AxisDirection crossAxisDirection = AxisDirection.right,
            ViewportOffset offset = null,
            double cacheExtent = RenderViewportUtils.defaultCacheExtent
        ) {
            D.assert(offset != null);
            D.assert(AxisUtils.axisDirectionToAxis(axisDirection) != AxisUtils.axisDirectionToAxis(crossAxisDirection));

            this._axisDirection = axisDirection;
            this._crossAxisDirection = crossAxisDirection;
            this._offset = offset;
            this._cacheExtent = cacheExtent;
        }
        
        public new RenderObject parent {
            get {
                return (RenderObject) base.parent;
            }
        }

        public AxisDirection axisDirection {
            get { return this._axisDirection; }
            set {
                if (value == this._axisDirection) {
                    return;
                }

                this._axisDirection = value;
                this.markNeedsLayout();
            }
        }

        AxisDirection _axisDirection;

        public AxisDirection crossAxisDirection {
            get { return this._crossAxisDirection; }
            set {
                if (value == this._crossAxisDirection) {
                    return;
                }

                this._crossAxisDirection = value;
                this.markNeedsLayout();
            }
        }

        AxisDirection _crossAxisDirection;

        public Axis axis {
            get { return AxisUtils.axisDirectionToAxis(this.axisDirection); }
        }

        public ViewportOffset offset {
            get { return this._offset; }
            set {
                D.assert(value != null);
                if (value == this._offset) {
                    return;
                }

                if (this.attached) {
                    this._offset.removeListener(this.markNeedsLayout);
                }

                this._offset = value;
                if (this.attached) {
                    this._offset.addListener(this.markNeedsLayout);
                }

                this.markNeedsLayout();
            }
        }

        ViewportOffset _offset;

        public double cacheExtent {
            get { return this._cacheExtent; }
            set {
                if (value == this._cacheExtent) {
                    return;
                }

                this._cacheExtent = value;
                this.markNeedsLayout();
            }
        }

        double _cacheExtent;

        public override void attach(object owner) {
            base.attach(owner);
            this._offset.addListener(this.markNeedsLayout);
        }

        public override void detach() {
            this._offset.removeListener(this.markNeedsLayout);
            base.detach();
        }

        protected virtual bool debugThrowIfNotCheckingIntrinsics() {
            D.assert(() => {
                if (!RenderObject.debugCheckingIntrinsics) {
                    D.assert(!(this is RenderShrinkWrappingViewport));
                    throw new UIWidgetsError(
                        this.GetType() + " does not support returning intrinsic dimensions.\n" +
                        "Calculating the intrinsic dimensions would require instantiating every child of " +
                        "the viewport, which defeats the point of viewports being lazy.\n" +
                        "If you are merely trying to shrink-wrap the viewport in the main axis direction, " +
                        "consider a RenderShrinkWrappingViewport render object (ShrinkWrappingViewport widget), " +
                        "which achieves that effect without implementing the intrinsic dimension API."
                    );
                }

                return true;
            });
            return true;
        }

        protected override double computeMinIntrinsicWidth(double height) {
            D.assert(this.debugThrowIfNotCheckingIntrinsics());
            return 0.0;
        }

        protected override double computeMaxIntrinsicWidth(double height) {
            D.assert(this.debugThrowIfNotCheckingIntrinsics());
            return 0.0;
        }

        protected override double computeMinIntrinsicHeight(double width) {
            D.assert(this.debugThrowIfNotCheckingIntrinsics());
            return 0.0;
        }

        protected override double computeMaxIntrinsicHeight(double width) {
            D.assert(this.debugThrowIfNotCheckingIntrinsics());
            return 0.0;
        }

        public override bool isRepaintBoundary {
            get { return true; }
        }

        protected double layoutChildSequence(
            RenderSliver child,
            double scrollOffset,
            double overlap,
            double layoutOffset,
            double remainingPaintExtent,
            double mainAxisExtent,
            double crossAxisExtent,
            GrowthDirection growthDirection,
            Func<RenderSliver, RenderSliver> advance,
            double remainingCacheExtent,
            double cacheOrigin
        ) {
            D.assert(scrollOffset.isFinite());
            D.assert(scrollOffset >= 0.0);

            double initialLayoutOffset = layoutOffset;
            ScrollDirection adjustedUserScrollDirection =
                GrowthDirectionUtils.applyGrowthDirectionToScrollDirection(
                    this.offset.userScrollDirection, growthDirection);
            double maxPaintOffset = layoutOffset + overlap;

            while (child != null) {
                double sliverScrollOffset = scrollOffset <= 0.0 ? 0.0 : scrollOffset;

                double correctedCacheOrigin = Math.Max(cacheOrigin, -sliverScrollOffset);
                double cacheExtentCorrection = cacheOrigin - correctedCacheOrigin;

                D.assert(sliverScrollOffset >= correctedCacheOrigin.abs());
                D.assert(correctedCacheOrigin <= 0.0);
                D.assert(sliverScrollOffset >= 0.0);
                D.assert(cacheExtentCorrection <= 0.0);

                child.layout(new SliverConstraints(
                    axisDirection: this.axisDirection,
                    growthDirection: growthDirection,
                    userScrollDirection: adjustedUserScrollDirection,
                    scrollOffset: sliverScrollOffset,
                    overlap: maxPaintOffset - layoutOffset,
                    remainingPaintExtent: Math.Max(0.0, remainingPaintExtent - layoutOffset + initialLayoutOffset),
                    crossAxisExtent: crossAxisExtent,
                    crossAxisDirection: this.crossAxisDirection,
                    viewportMainAxisExtent: mainAxisExtent,
                    remainingCacheExtent: Math.Max(0.0, remainingCacheExtent + cacheExtentCorrection),
                    cacheOrigin: correctedCacheOrigin
                ), parentUsesSize: true);

                var childLayoutGeometry = child.geometry;
                D.assert(childLayoutGeometry.debugAssertIsValid());

                if (childLayoutGeometry.scrollOffsetCorrection != null) {
                    return childLayoutGeometry.scrollOffsetCorrection.Value;
                }

                double effectiveLayoutOffset = layoutOffset + childLayoutGeometry.paintOrigin;

                if (childLayoutGeometry.visible || scrollOffset > 0) {
                    this.updateChildLayoutOffset(child, effectiveLayoutOffset, growthDirection);
                } else {
                    this.updateChildLayoutOffset(child, -scrollOffset + initialLayoutOffset, growthDirection);
                }

                maxPaintOffset = Math.Max(effectiveLayoutOffset + childLayoutGeometry.paintExtent, maxPaintOffset);
                scrollOffset -= childLayoutGeometry.scrollExtent;
                layoutOffset += childLayoutGeometry.layoutExtent;

                if (childLayoutGeometry.cacheExtent != 0.0) {
                    remainingCacheExtent -= childLayoutGeometry.cacheExtent - cacheExtentCorrection;
                    cacheOrigin = Math.Min(correctedCacheOrigin + childLayoutGeometry.cacheExtent, 0.0);
                }

                this.updateOutOfBandData(growthDirection, childLayoutGeometry);

                child = advance(child);
            }

            return 0.0;
        }

        public override Rect describeApproximatePaintClip(RenderObject childRaw) {
            RenderSliver child = (RenderSliver) childRaw;

            Rect viewportClip = Offset.zero & this.size;
            if (child.constraints.overlap == 0.0) {
                return viewportClip;
            }

            double left = viewportClip.left;
            double right = viewportClip.right;
            double top = viewportClip.top;
            double bottom = viewportClip.bottom;
            double startOfOverlap = child.constraints.viewportMainAxisExtent - child.constraints.remainingPaintExtent;
            double overlapCorrection = startOfOverlap + child.constraints.overlap;
            switch (GrowthDirectionUtils.applyGrowthDirectionToAxisDirection(
                this.axisDirection, child.constraints.growthDirection)) {
                case AxisDirection.down:
                    top += overlapCorrection;
                    break;
                case AxisDirection.up:
                    bottom -= overlapCorrection;
                    break;
                case AxisDirection.right:
                    left += overlapCorrection;
                    break;
                case AxisDirection.left:
                    right -= overlapCorrection;
                    break;
            }

            return Rect.fromLTRB(left, top, right, bottom);
        }

        public override void paint(PaintingContext context, Offset offset) {
            if (this.firstChild == null) {
                return;
            }

            if (this.hasVisualOverflow) {
                context.pushClipRect(this.needsCompositing, offset, Offset.zero & this.size, this._paintContents);
            } else {
                this._paintContents(context, offset);
            }
        }

        public void _paintContents(PaintingContext context, Offset offset) {
            foreach (RenderSliver child in this.childrenInPaintOrder) {
                if (child.geometry.visible) {
                    context.paintChild(child, offset + this.paintOffsetOf(child));
                }
            }
        }

        protected override void debugPaintSize(PaintingContext context, Offset offset) {
            D.assert(() => {
                base.debugPaintSize(context, offset);

                Paint paint = new Paint {
                    color = new Color(0xFF00FF00)
                };

                Canvas canvas = context.canvas;
                RenderSliver child = this.firstChild;
                while (child != null) {
                    Size size = null;
                    switch (this.axis) {
                        case Axis.vertical:
                            size = new Size(child.constraints.crossAxisExtent, child.geometry.layoutExtent);
                            break;
                        case Axis.horizontal:
                            size = new Size(child.geometry.layoutExtent, child.constraints.crossAxisExtent);
                            break;
                    }

                    D.assert(size != null);
//                    canvas.drawRect(((offset + this.paintOffsetOf(child)) & size).deflate(0.5),
//                        BorderWidth.all(1), BorderRadius.zero, paint);
                    child = this.childAfter(child);
                }

                return true;
            });
        }

        protected override bool hitTestChildren(HitTestResult result, Offset position = null) {
            D.assert(position != null);

            double mainAxisPosition = 0, crossAxisPosition = 0;
            switch (this.axis) {
                case Axis.vertical:
                    mainAxisPosition = position.dy;
                    crossAxisPosition = position.dx;
                    break;
                case Axis.horizontal:
                    mainAxisPosition = position.dx;
                    crossAxisPosition = position.dy;
                    break;
            }

            foreach (RenderSliver child in this.childrenInHitTestOrder) {
                if (child.geometry.visible && child.hitTest(
                        result,
                        mainAxisPosition: this.computeChildMainAxisPosition(child, mainAxisPosition),
                        crossAxisPosition: crossAxisPosition
                    )) {
                    return true;
                }
            }

            return false;
        }

        public RevealedOffset getOffsetToReveal(RenderObject target, double alignment, Rect rect = null) {
            double leadingScrollOffset = 0.0;
            double targetMainAxisExtent = 0.0;
            RenderObject descendant;
            rect = rect ?? target.paintBounds;

            Matrix3 transform;

            if (target is RenderBox) {
                RenderBox targetBox = (RenderBox) target;

                RenderBox pivot = targetBox;
                while (pivot.parent is RenderBox) {
                    pivot = (RenderBox) pivot.parent;
                }

                D.assert(pivot.parent != null);
                D.assert(pivot.parent != this);
                D.assert(pivot != this);
                D.assert(pivot.parent is RenderSliver);

                RenderSliver pivotParent = (RenderSliver) pivot.parent;

                transform = targetBox.getTransformTo(pivot);
                Rect bounds = transform.transformRect(rect);

                double offset = 0.0;

                GrowthDirection growthDirection = pivotParent.constraints.growthDirection;
                switch (GrowthDirectionUtils.applyGrowthDirectionToAxisDirection(this.axisDirection, growthDirection)) {
                    case AxisDirection.up:
                        switch (growthDirection) {
                            case GrowthDirection.forward:
                                offset = bounds.bottom;
                                break;
                            case GrowthDirection.reverse:
                                offset = bounds.top;
                                break;
                        }

                        leadingScrollOffset = pivot.size.height - offset;
                        targetMainAxisExtent = bounds.height;
                        break;
                    case AxisDirection.right:
                        leadingScrollOffset = bounds.left;
                        targetMainAxisExtent = bounds.width;
                        break;
                    case AxisDirection.down:
                        leadingScrollOffset = bounds.top;
                        targetMainAxisExtent = bounds.height;
                        break;
                    case AxisDirection.left:
                        switch (growthDirection) {
                            case GrowthDirection.forward:
                                offset = bounds.right;
                                break;
                            case GrowthDirection.reverse:
                                offset = bounds.left;
                                break;
                        }

                        leadingScrollOffset = pivot.size.width - offset;
                        targetMainAxisExtent = bounds.width;
                        break;
                }

                descendant = pivot;
            } else if (target is RenderSliver) {
                RenderSliver targetSliver = (RenderSliver) target;
                leadingScrollOffset = 0.0;
                targetMainAxisExtent = targetSliver.geometry.scrollExtent;
                descendant = targetSliver;
            } else {
                return new RevealedOffset(offset: this.offset.pixels, rect: rect);
            }

            RenderObject child = descendant;
            while (child.parent is RenderSliver) {
                var parent = (RenderSliver) child.parent;
                leadingScrollOffset += parent.childScrollOffset(child);
                child = parent;
            }

            D.assert(child.parent == this);
            D.assert(child is RenderSliver);

            RenderSliver sliver = (RenderSliver) child;
            double extentOfPinnedSlivers = this.maxScrollObstructionExtentBefore(sliver);
            leadingScrollOffset = this.scrollOffsetOf(sliver, leadingScrollOffset);
            switch (sliver.constraints.growthDirection) {
                case GrowthDirection.forward:
                    leadingScrollOffset -= extentOfPinnedSlivers;
                    break;
                case GrowthDirection.reverse:
                    break;
            }

            double mainAxisExtent = 0.0;
            switch (this.axis) {
                case Axis.horizontal:
                    mainAxisExtent = this.size.width - extentOfPinnedSlivers;
                    break;
                case Axis.vertical:
                    mainAxisExtent = this.size.height - extentOfPinnedSlivers;
                    break;
            }

            double targetOffset = leadingScrollOffset - (mainAxisExtent - targetMainAxisExtent) * alignment;
            double offsetDifference = this.offset.pixels - targetOffset;

            transform = target.getTransformTo(this);
            this.applyPaintTransform(child, ref transform);
            Rect targetRect = transform.transformRect(rect);

            switch (this.axisDirection) {
                case AxisDirection.down:
                    targetRect = targetRect.translate(0.0, offsetDifference);
                    break;
                case AxisDirection.right:
                    targetRect = targetRect.translate(offsetDifference, 0.0);
                    break;
                case AxisDirection.up:
                    targetRect = targetRect.translate(0.0, -offsetDifference);
                    break;
                case AxisDirection.left:
                    targetRect = targetRect.translate(-offsetDifference, 0.0);
                    break;
            }

            return new RevealedOffset(offset: targetOffset, rect: targetRect);
        }

        protected Offset computeAbsolutePaintOffset(RenderSliver child, double layoutOffset,
            GrowthDirection growthDirection) {
            D.assert(this.hasSize);
            D.assert(child != null);
            D.assert(child.geometry != null);

            switch (GrowthDirectionUtils.applyGrowthDirectionToAxisDirection(this.axisDirection, growthDirection)) {
                case AxisDirection.up:
                    return new Offset(0.0, this.size.height - (layoutOffset + child.geometry.paintExtent));
                case AxisDirection.right:
                    return new Offset(layoutOffset, 0.0);
                case AxisDirection.down:
                    return new Offset(0.0, layoutOffset);
                case AxisDirection.left:
                    return new Offset(this.size.width - (layoutOffset + child.geometry.paintExtent), 0.0);
            }

            return null;
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(new EnumProperty<AxisDirection>("axisDirection", this.axisDirection));
            properties.add(new EnumProperty<AxisDirection>("crossAxisDirection", this.crossAxisDirection));
            properties.add(new DiagnosticsProperty<ViewportOffset>("offset", this.offset));
        }

        public override List<DiagnosticsNode> debugDescribeChildren() {
            var children = new List<DiagnosticsNode>();
            RenderSliver child = this.firstChild;
            if (child == null) {
                return children;
            }

            int count = this.indexOfFirstChild;
            while (true) {
                children.Add(child.toDiagnosticsNode(name: this.labelForChild(count)));
                if (child == this.lastChild)
                    break;
                count += 1;
                child = this.childAfter(child);
            }

            return children;
        }


        protected abstract bool hasVisualOverflow { get; }

        protected abstract void
            updateOutOfBandData(GrowthDirection growthDirection, SliverGeometry childLayoutGeometry);

        protected abstract void updateChildLayoutOffset(RenderSliver child, double layoutOffset,
            GrowthDirection growthDirection);

        protected abstract Offset paintOffsetOf(RenderSliver child);

        protected abstract double scrollOffsetOf(RenderSliver child, double scrollOffsetWithinChild);

        protected abstract double maxScrollObstructionExtentBefore(RenderSliver child);

        protected abstract double computeChildMainAxisPosition(RenderSliver child, double parentMainAxisPosition);

        protected abstract int indexOfFirstChild { get; }

        protected abstract string labelForChild(int index);

        protected abstract IEnumerable<RenderSliver> childrenInPaintOrder { get; }

        protected abstract IEnumerable<RenderSliver> childrenInHitTestOrder { get; }

        public override void showOnScreen(
            RenderObject descendant,
            Rect rect,
            TimeSpan? duration = null,
            Curve curve = null
        ) {
            duration = duration ?? TimeSpan.Zero;
            curve = curve ?? Curves.ease;

            if (!this.offset.allowImplicitScrolling) {
                base.showOnScreen(
                    descendant: descendant,
                    rect: rect,
                    duration: duration,
                    curve: curve
                );
                return;
            }

            Rect newRect = showInViewport(
                descendant: descendant,
                viewport: this,
                offset: this.offset,
                rect: rect,
                duration: duration,
                curve: curve
            );
            base.showOnScreen(
                rect: newRect,
                duration: duration,
                curve: curve
            );
        }

        public static Rect showInViewport(
            RenderObject descendant = null,
            Rect rect = null,
            RenderAbstractViewport viewport = null,
            ViewportOffset offset = null,
            TimeSpan? duration = null,
            Curve curve = null
        ) {
            duration = duration ?? TimeSpan.Zero;
            curve = curve ?? Curves.ease;
            D.assert(viewport != null);
            D.assert(offset != null);
            if (descendant == null) {
                return rect;
            }

            RevealedOffset leadingEdgeOffset = viewport.getOffsetToReveal(descendant, 0.0, rect: rect);
            RevealedOffset trailingEdgeOffset = viewport.getOffsetToReveal(descendant, 1.0, rect: rect);
            double currentOffset = offset.pixels;


            RevealedOffset targetOffset = null;
            if (leadingEdgeOffset.offset < trailingEdgeOffset.offset) {
                double leadingEdgeDiff = (offset.pixels - leadingEdgeOffset.offset).abs();
                double trailingEdgeDiff = (offset.pixels - trailingEdgeOffset.offset).abs();
                targetOffset = leadingEdgeDiff < trailingEdgeDiff ? leadingEdgeOffset : trailingEdgeOffset;
            } else if (currentOffset > leadingEdgeOffset.offset) {
                targetOffset = leadingEdgeOffset;
            } else if (currentOffset < trailingEdgeOffset.offset) {
                targetOffset = trailingEdgeOffset;
            } else {
                var transform = descendant.getTransformTo(viewport.parent);
                return transform.transformRect(rect ?? descendant.paintBounds);
            }

            D.assert(targetOffset != null);

            if (duration == TimeSpan.Zero) {
                offset.jumpTo(targetOffset.offset);
            } else {
                offset.animateTo(targetOffset.offset, duration: duration.Value, curve: curve);
            }

            return targetOffset.rect;
        }
    }


    public class RenderViewport : RenderViewportBase<SliverPhysicalContainerParentData> {
        public RenderViewport(
            AxisDirection axisDirection = AxisDirection.down,
            AxisDirection crossAxisDirection = AxisDirection.right,
            ViewportOffset offset = null,
            double anchor = 0.0,
            List<RenderSliver> children = null,
            RenderSliver center = null,
            double cacheExtent = RenderViewportUtils.defaultCacheExtent
        ) : base(axisDirection, crossAxisDirection, offset, cacheExtent) {
            D.assert(anchor >= 0.0 && anchor <= 1.0);
            this._anchor = anchor;
            this._center = center;

            this.addAll(children);

            if (center == null && this.firstChild != null) {
                this._center = this.firstChild;
            }
        }

        public override void setupParentData(RenderObject child) {
            if (!(child.parentData is SliverPhysicalContainerParentData)) {
                child.parentData = new SliverPhysicalContainerParentData();
            }
        }

        public double anchor {
            get { return this._anchor; }
            set {
                D.assert(value >= 0.0 && value <= 1.0);

                if (value == this._anchor) {
                    return;
                }

                this._anchor = value;
                this.markNeedsLayout();
            }
        }

        public double _anchor;

        public RenderSliver center {
            get { return this._center; }
            set {
                if (value == this._center) {
                    return;
                }

                this._center = value;
                this.markNeedsLayout();
            }
        }

        public RenderSliver _center;

        protected override bool sizedByParent {
            get { return true; }
        }

        protected override void performResize() {
            D.assert(() => {
                if (!this.constraints.hasBoundedHeight || !this.constraints.hasBoundedWidth) {
                    switch (this.axis) {
                        case Axis.vertical:
                            if (!this.constraints.hasBoundedHeight) {
                                throw new UIWidgetsError(
                                    "Vertical viewport was given unbounded height.\n" +
                                    "Viewports expand in the scrolling direction to fill their container." +
                                    "In this case, a vertical viewport was given an unlimited amount of " +
                                    "vertical space in which to expand. This situation typically happens " +
                                    "when a scrollable widget is nested inside another scrollable widget.\n" +
                                    "If this widget is always nested in a scrollable widget there " +
                                    "is no need to use a viewport because there will always be enough " +
                                    "vertical space for the children. In this case, consider using a " +
                                    "Column instead. Otherwise, consider using the \"shrinkWrap\" property " +
                                    "(or a ShrinkWrappingViewport) to size the height of the viewport " +
                                    "to the sum of the heights of its children."
                                );
                            }

                            if (!this.constraints.hasBoundedWidth) {
                                throw new UIWidgetsError(
                                    "Vertical viewport was given unbounded width.\n" +
                                    "Viewports expand in the cross axis to fill their container and " +
                                    "constrain their children to match their extent in the cross axis. " +
                                    "In this case, a vertical viewport was given an unlimited amount of " +
                                    "horizontal space in which to expand."
                                );
                            }

                            break;
                        case Axis.horizontal:
                            if (!this.constraints.hasBoundedWidth) {
                                throw new UIWidgetsError(
                                    "Horizontal viewport was given unbounded width.\n" +
                                    "Viewports expand in the scrolling direction to fill their container." +
                                    "In this case, a horizontal viewport was given an unlimited amount of " +
                                    "horizontal space in which to expand. This situation typically happens " +
                                    "when a scrollable widget is nested inside another scrollable widget.\n" +
                                    "If this widget is always nested in a scrollable widget there " +
                                    "is no need to use a viewport because there will always be enough " +
                                    "horizontal space for the children. In this case, consider using a " +
                                    "Row instead. Otherwise, consider using the \"shrinkWrap\" property " +
                                    "(or a ShrinkWrappingViewport) to size the width of the viewport " +
                                    "to the sum of the widths of its children."
                                );
                            }

                            if (!this.constraints.hasBoundedHeight) {
                                throw new UIWidgetsError(
                                    "Horizontal viewport was given unbounded height.\n" +
                                    "Viewports expand in the cross axis to fill their container and " +
                                    "constrain their children to match their extent in the cross axis. " +
                                    "In this case, a horizontal viewport was given an unlimited amount of " +
                                    "vertical space in which to expand."
                                );
                            }

                            break;
                    }
                }

                return true;
            });

            this.size = this.constraints.biggest;

            switch (this.axis) {
                case Axis.vertical:
                    this.offset.applyViewportDimension(this.size.height);
                    break;
                case Axis.horizontal:
                    this.offset.applyViewportDimension(this.size.width);
                    break;
            }
        }

        const int _maxLayoutCycles = 10;

        double _minScrollExtent;
        double _maxScrollExtent;
        bool _hasVisualOverflow = false;

        protected override void performLayout() {
            if (this.center == null) {
                D.assert(this.firstChild == null);
                this._minScrollExtent = 0.0;
                this._maxScrollExtent = 0.0;
                this._hasVisualOverflow = false;
                this.offset.applyContentDimensions(0.0, 0.0);
                return;
            }

            double mainAxisExtent = 0.0;
            double crossAxisExtent = 0.0;
            switch (this.axis) {
                case Axis.vertical:
                    mainAxisExtent = this.size.height;
                    crossAxisExtent = this.size.width;
                    break;
                case Axis.horizontal:
                    mainAxisExtent = this.size.width;
                    crossAxisExtent = this.size.height;
                    break;
            }

            double centerOffsetAdjustment = this.center.centerOffsetAdjustment;

            int count = 0;
            do {
                var correction = this._attemptLayout(mainAxisExtent, crossAxisExtent,
                    this.offset.pixels + centerOffsetAdjustment);
                if (correction != 0.0) {
                    this.offset.correctBy(correction);
                } else {
                    if (this.offset.applyContentDimensions(
                        Math.Min(0.0, this._minScrollExtent + mainAxisExtent * this.anchor),
                        Math.Max(0.0, this._maxScrollExtent - mainAxisExtent * (1.0 - this.anchor))
                    ))
                        break;
                }

                count += 1;
            } while (count < _maxLayoutCycles);

            D.assert(() => {
                if (count >= _maxLayoutCycles) {
                    D.assert(count != 1);
                    throw new UIWidgetsError(
                        "A RenderViewport exceeded its maximum number of layout cycles.\n" +
                        "RenderViewport render objects, during layout, can retry if either their " +
                        "slivers or their ViewportOffset decide that the offset should be corrected " +
                        "to take into account information collected during that layout.\n" +
                        "In the case of this RenderViewport object, however, this happened $count " +
                        "times and still there was no consensus on the scroll offset. This usually " +
                        "indicates a bug. Specifically, it means that one of the following three " +
                        "problems is being experienced by the RenderViewport object:\n" +
                        " * One of the RenderSliver children or the ViewportOffset have a bug such" +
                        " that they always think that they need to correct the offset regardless.\n" +
                        " * Some combination of the RenderSliver children and the ViewportOffset" +
                        " have a bad interaction such that one applies a correction then another" +
                        " applies a reverse correction, leading to an infinite loop of corrections.\n" +
                        " * There is a pathological case that would eventually resolve, but it is" +
                        " so complicated that it cannot be resolved in any reasonable number of" +
                        " layout passes."
                    );
                }

                return true;
            });
        }

        double _attemptLayout(double mainAxisExtent, double crossAxisExtent, double correctedOffset) {
            D.assert(!mainAxisExtent.isNaN());
            D.assert(mainAxisExtent >= 0.0);
            D.assert(crossAxisExtent.isFinite());
            D.assert(crossAxisExtent >= 0.0);
            D.assert(correctedOffset.isFinite());

            this._minScrollExtent = 0.0;
            this._maxScrollExtent = 0.0;
            this._hasVisualOverflow = false;

            double centerOffset = mainAxisExtent * this.anchor - correctedOffset;
            double reverseDirectionRemainingPaintExtent = centerOffset.clamp(0.0, mainAxisExtent);
            double forwardDirectionRemainingPaintExtent = (mainAxisExtent - centerOffset).clamp(0.0, mainAxisExtent);

            double fullCacheExtent = mainAxisExtent + 2 * this.cacheExtent;
            double centerCacheOffset = centerOffset + this.cacheExtent;
            double reverseDirectionRemainingCacheExtent = centerCacheOffset.clamp(0.0, fullCacheExtent);
            double forwardDirectionRemainingCacheExtent =
                (fullCacheExtent - centerCacheOffset).clamp(0.0, fullCacheExtent);

            RenderSliver leadingNegativeChild = this.childBefore(this.center);

            if (leadingNegativeChild != null) {
                double result = this.layoutChildSequence(
                    child: leadingNegativeChild,
                    scrollOffset: Math.Max(mainAxisExtent, centerOffset) - mainAxisExtent,
                    overlap: 0.0,
                    layoutOffset: forwardDirectionRemainingPaintExtent,
                    remainingPaintExtent: reverseDirectionRemainingPaintExtent,
                    mainAxisExtent: mainAxisExtent,
                    crossAxisExtent: crossAxisExtent,
                    growthDirection: GrowthDirection.reverse,
                    advance: this.childBefore,
                    remainingCacheExtent: reverseDirectionRemainingCacheExtent,
                    cacheOrigin: (mainAxisExtent - centerOffset).clamp(-this.cacheExtent, 0.0)
                );
                if (result != 0.0) {
                    return -result;
                }
            }

            return this.layoutChildSequence(
                child: this.center,
                scrollOffset: Math.Max(0.0, -centerOffset),
                overlap: leadingNegativeChild == null ? Math.Min(0.0, -centerOffset) : 0.0,
                layoutOffset: centerOffset >= mainAxisExtent ? centerOffset : reverseDirectionRemainingPaintExtent,
                remainingPaintExtent: forwardDirectionRemainingPaintExtent,
                mainAxisExtent: mainAxisExtent,
                crossAxisExtent: crossAxisExtent,
                growthDirection: GrowthDirection.forward,
                advance: this.childAfter,
                remainingCacheExtent: forwardDirectionRemainingCacheExtent,
                cacheOrigin: centerOffset.clamp(-this.cacheExtent, 0.0)
            );
        }

        protected override bool hasVisualOverflow {
            get { return this._hasVisualOverflow; }
        }

        protected override void
            updateOutOfBandData(GrowthDirection growthDirection, SliverGeometry childLayoutGeometry) {
            switch (growthDirection) {
                case GrowthDirection.forward:
                    this._maxScrollExtent += childLayoutGeometry.scrollExtent;
                    break;
                case GrowthDirection.reverse:
                    this._minScrollExtent -= childLayoutGeometry.scrollExtent;
                    break;
            }

            if (childLayoutGeometry.hasVisualOverflow) {
                this._hasVisualOverflow = true;
            }
        }

        protected override void updateChildLayoutOffset(RenderSliver child, double layoutOffset,
            GrowthDirection growthDirection) {
            var childParentData = (SliverPhysicalParentData) child.parentData;
            childParentData.paintOffset = this.computeAbsolutePaintOffset(child, layoutOffset, growthDirection);
        }

        protected override Offset paintOffsetOf(RenderSliver child) {
            var childParentData = (SliverPhysicalParentData) child.parentData;
            return childParentData.paintOffset;
        }

        protected override double scrollOffsetOf(RenderSliver child, double scrollOffsetWithinChild) {
            D.assert(child.parent == this);

            GrowthDirection growthDirection = child.constraints.growthDirection;
            switch (growthDirection) {
                case GrowthDirection.forward: {
                    double scrollOffsetToChild = 0.0;
                    RenderSliver current = this.center;
                    while (current != child) {
                        scrollOffsetToChild += current.geometry.scrollExtent;
                        current = this.childAfter(current);
                    }

                    return scrollOffsetToChild + scrollOffsetWithinChild;
                }
                case GrowthDirection.reverse: {
                    double scrollOffsetToChild = 0.0;
                    RenderSliver current = this.childBefore(this.center);
                    while (current != child) {
                        scrollOffsetToChild -= current.geometry.scrollExtent;
                        current = this.childBefore(current);
                    }

                    return scrollOffsetToChild - scrollOffsetWithinChild;
                }
            }

            D.assert(false);
            return 0.0;
        }

        protected override double maxScrollObstructionExtentBefore(RenderSliver child) {
            D.assert(child.parent == this);

            GrowthDirection growthDirection = child.constraints.growthDirection;
            switch (growthDirection) {
                case GrowthDirection.forward: {
                    double pinnedExtent = 0.0;
                    RenderSliver current = this.center;
                    while (current != child) {
                        pinnedExtent += current.geometry.maxScrollObstructionExtent;
                        current = this.childAfter(current);
                    }

                    return pinnedExtent;
                }
                case GrowthDirection.reverse: {
                    double pinnedExtent = 0.0;
                    RenderSliver current = this.childBefore(this.center);
                    while (current != child) {
                        pinnedExtent += current.geometry.maxScrollObstructionExtent;
                        current = this.childBefore(current);
                    }

                    return pinnedExtent;
                }
            }

            D.assert(false);
            return 0.0;
        }

        public override void applyPaintTransform(RenderObject child, ref Matrix3 transform) {
            D.assert(child != null);

            var childParentData = (SliverPhysicalParentData) child.parentData;
            childParentData.applyPaintTransform(ref transform);
        }

        protected override double computeChildMainAxisPosition(RenderSliver child, double parentMainAxisPosition) {
            D.assert(child != null);
            D.assert(child.constraints != null);
            SliverPhysicalParentData childParentData = (SliverPhysicalParentData) child.parentData;
            switch (GrowthDirectionUtils.applyGrowthDirectionToAxisDirection(child.constraints.axisDirection,
                child.constraints.growthDirection)) {
                case AxisDirection.down:
                    return parentMainAxisPosition - childParentData.paintOffset.dy;
                case AxisDirection.right:
                    return parentMainAxisPosition - childParentData.paintOffset.dx;
                case AxisDirection.up:
                    return child.geometry.paintExtent - (parentMainAxisPosition - childParentData.paintOffset.dy);
                case AxisDirection.left:
                    return child.geometry.paintExtent - (parentMainAxisPosition - childParentData.paintOffset.dx);
            }

            D.assert(false);
            return 0.0;
        }

        protected override int indexOfFirstChild {
            get {
                D.assert(this.center != null);
                D.assert(this.center.parent == this);
                D.assert(this.firstChild != null);
                int count = 0;
                RenderSliver child = this.center;
                while (child != this.firstChild) {
                    count -= 1;
                    child = this.childBefore(child);
                }

                return count;
            }
        }

        protected override string labelForChild(int index) {
            if (index == 0) {
                return "center child";
            }

            return "child " + index;
        }

        protected override IEnumerable<RenderSliver> childrenInPaintOrder {
            get {
                if (this.firstChild == null) {
                    yield break;
                }

                var child = this.firstChild;
                while (child != this.center) {
                    yield return child;
                    child = this.childAfter(child);
                }

                child = this.lastChild;
                while (true) {
                    yield return child;
                    if (child == this.center) {
                        yield break;
                    }

                    child = this.childBefore(child);
                }
            }
        }

        protected override IEnumerable<RenderSliver> childrenInHitTestOrder {
            get {
                if (this.firstChild == null) {
                    yield break;
                }

                RenderSliver child = this.center;
                while (child != null) {
                    yield return child;
                    child = this.childAfter(child);
                }

                child = this.childBefore(this.center);
                while (child != null) {
                    yield return child;
                    child = this.childBefore(child);
                }
            }
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(new DoubleProperty("anchor", this.anchor));
        }
    }

    public class RenderShrinkWrappingViewport : RenderViewportBase<SliverLogicalContainerParentData> {
        public RenderShrinkWrappingViewport(
            AxisDirection axisDirection = AxisDirection.down,
            AxisDirection crossAxisDirection = AxisDirection.right,
            ViewportOffset offset = null,
            List<RenderSliver> children = null
        ) : base(
            axisDirection: axisDirection,
            crossAxisDirection: crossAxisDirection,
            offset: offset) {
            this.addAll(children);
        }

        public override void setupParentData(RenderObject child) {
            if (!(child.parentData is SliverLogicalContainerParentData)) {
                child.parentData = new SliverLogicalContainerParentData();
            }
        }

        protected override bool debugThrowIfNotCheckingIntrinsics() {
            D.assert(() => {
                if (!RenderObject.debugCheckingIntrinsics) {
                    throw new UIWidgetsError(
                        this.GetType() + " does not support returning intrinsic dimensions.\n" +
                        "Calculating the intrinsic dimensions would require instantiating every child of " +
                        "the viewport, which defeats the point of viewports being lazy.\n" +
                        "If you are merely trying to shrink-wrap the viewport in the main axis direction, " +
                        "you should be able to achieve that effect by just giving the viewport loose " +
                        "constraints, without needing to measure its intrinsic dimensions."
                    );
                }

                return true;
            });
            return true;
        }

        double _maxScrollExtent = 0.0;
        double _shrinkWrapExtent = 0.0;
        bool _hasVisualOverflow = false;

        protected override void performLayout() {
            if (this.firstChild == null) {
                switch (this.axis) {
                    case Axis.vertical:
                        D.assert(this.constraints.hasBoundedWidth);
                        this.size = new Size(this.constraints.maxWidth, this.constraints.minHeight);
                        break;
                    case Axis.horizontal:
                        D.assert(this.constraints.hasBoundedHeight);
                        this.size = new Size(this.constraints.minWidth, this.constraints.maxHeight);
                        break;
                }

                this.offset.applyViewportDimension(0.0);
                this._maxScrollExtent = 0.0;
                this._shrinkWrapExtent = 0.0;
                this._hasVisualOverflow = false;
                this.offset.applyContentDimensions(0.0, 0.0);
                return;
            }

            double mainAxisExtent = 0.0;
            double crossAxisExtent = 0.0;
            switch (this.axis) {
                case Axis.vertical:
                    D.assert(this.constraints.hasBoundedWidth);
                    mainAxisExtent = this.constraints.maxHeight;
                    crossAxisExtent = this.constraints.maxWidth;
                    break;
                case Axis.horizontal:
                    D.assert(this.constraints.hasBoundedHeight);
                    mainAxisExtent = this.constraints.maxWidth;
                    crossAxisExtent = this.constraints.maxHeight;
                    break;
            }

            double effectiveExtent = 0.0;
            do {
                var correction = this._attemptLayout(mainAxisExtent, crossAxisExtent, this.offset.pixels);
                if (correction != 0.0) {
                    this.offset.correctBy(correction);
                } else {
                    switch (this.axis) {
                        case Axis.vertical:
                            effectiveExtent = this.constraints.constrainHeight(this._shrinkWrapExtent);
                            break;
                        case Axis.horizontal:
                            effectiveExtent = this.constraints.constrainWidth(this._shrinkWrapExtent);
                            break;
                    }

                    bool didAcceptViewportDimension = this.offset.applyViewportDimension(effectiveExtent);
                    bool didAcceptContentDimension =
                        this.offset.applyContentDimensions(0.0, Math.Max(0.0, this._maxScrollExtent - effectiveExtent));
                    if (didAcceptViewportDimension && didAcceptContentDimension) {
                        break;
                    }
                }
            } while (true);

            switch (this.axis) {
                case Axis.vertical:
                    this.size = this.constraints.constrainDimensions(crossAxisExtent, effectiveExtent);
                    break;
                case Axis.horizontal:
                    this.size = this.constraints.constrainDimensions(effectiveExtent, crossAxisExtent);
                    break;
            }
        }

        double _attemptLayout(double mainAxisExtent, double crossAxisExtent, double correctedOffset) {
            D.assert(!mainAxisExtent.isNaN());
            D.assert(mainAxisExtent >= 0.0);
            D.assert(crossAxisExtent.isFinite());
            D.assert(crossAxisExtent >= 0.0);
            D.assert(correctedOffset.isFinite());

            this._maxScrollExtent = 0.0;
            this._shrinkWrapExtent = 0.0;
            this._hasVisualOverflow = false;

            return this.layoutChildSequence(
                child: this.firstChild,
                scrollOffset: Math.Max(0.0, correctedOffset),
                overlap: Math.Min(0.0, correctedOffset),
                layoutOffset: 0.0,
                remainingPaintExtent: mainAxisExtent,
                mainAxisExtent: mainAxisExtent,
                crossAxisExtent: crossAxisExtent,
                growthDirection: GrowthDirection.forward,
                advance: this.childAfter,
                remainingCacheExtent: mainAxisExtent + 2 * this.cacheExtent,
                cacheOrigin: -this.cacheExtent
            );
        }

        protected override bool hasVisualOverflow {
            get { return this._hasVisualOverflow; }
        }

        protected override void
            updateOutOfBandData(GrowthDirection growthDirection, SliverGeometry childLayoutGeometry) {
            D.assert(growthDirection == GrowthDirection.forward);

            this._maxScrollExtent += childLayoutGeometry.scrollExtent;
            if (childLayoutGeometry.hasVisualOverflow) {
                this._hasVisualOverflow = true;
            }

            this._shrinkWrapExtent += childLayoutGeometry.maxPaintExtent;
        }

        protected override void updateChildLayoutOffset(RenderSliver child, double layoutOffset,
            GrowthDirection growthDirection) {
            D.assert(growthDirection == GrowthDirection.forward);

            var childParentData = (SliverLogicalParentData) child.parentData;
            childParentData.layoutOffset = layoutOffset;
        }

        protected override Offset paintOffsetOf(RenderSliver child) {
            var childParentData = (SliverLogicalParentData) child.parentData;
            return this.computeAbsolutePaintOffset(child, childParentData.layoutOffset, GrowthDirection.forward);
        }

        protected override double scrollOffsetOf(RenderSliver child, double scrollOffsetWithinChild) {
            D.assert(child.parent == this);
            D.assert(child.constraints.growthDirection == GrowthDirection.forward);

            double scrollOffsetToChild = 0.0;
            RenderSliver current = this.firstChild;
            while (current != child) {
                scrollOffsetToChild += current.geometry.scrollExtent;
                current = this.childAfter(current);
            }

            return scrollOffsetToChild + scrollOffsetWithinChild;
        }

        protected override double maxScrollObstructionExtentBefore(RenderSliver child) {
            D.assert(child.parent == this);
            D.assert(child.constraints.growthDirection == GrowthDirection.forward);

            double pinnedExtent = 0.0;
            RenderSliver current = this.firstChild;
            while (current != child) {
                pinnedExtent += current.geometry.maxScrollObstructionExtent;
                current = this.childAfter(current);
            }

            return pinnedExtent;
        }

        public override void applyPaintTransform(RenderObject child, ref Matrix3 transform) {
            D.assert(child != null);

            Offset offset = this.paintOffsetOf((RenderSliver) child);
            transform = MatrixUtils.makeTrans(offset.toVector()) * transform;
        }

        protected override double computeChildMainAxisPosition(RenderSliver child, double parentMainAxisPosition) {
            D.assert(child != null);
            D.assert(child.constraints != null);
            D.assert(this.hasSize);
            SliverLogicalParentData childParentData = (SliverLogicalParentData) child.parentData;
            switch (GrowthDirectionUtils.applyGrowthDirectionToAxisDirection(
                child.constraints.axisDirection, child.constraints.growthDirection)) {
                case AxisDirection.down:
                case AxisDirection.right:
                    return parentMainAxisPosition - childParentData.layoutOffset;
                case AxisDirection.up:
                    return (this.size.height - parentMainAxisPosition) - childParentData.layoutOffset;
                case AxisDirection.left:
                    return (this.size.width - parentMainAxisPosition) - childParentData.layoutOffset;
            }

            D.assert(false);
            return 0.0;
        }

        protected override int indexOfFirstChild {
            get { return 0; }
        }

        protected override string labelForChild(int index) {
            return "child " + index;
        }


        protected override IEnumerable<RenderSliver> childrenInPaintOrder {
            get {
                RenderSliver child = this.firstChild;
                while (child != null) {
                    yield return child;
                    child = this.childAfter(child);
                }
            }
        }

        protected override IEnumerable<RenderSliver> childrenInHitTestOrder {
            get {
                RenderSliver child = this.lastChild;
                while (child != null) {
                    yield return child;
                    child = this.childBefore(child);
                }
            }
        }
    }
}