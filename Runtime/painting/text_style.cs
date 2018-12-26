﻿using System;
using System.Collections.Generic;
using System.Linq;
using UIWidgets.foundation;
using UIWidgets.painting;
using UIWidgets.ui;

namespace UIWidgets.painting
{
    public class TextStyle : Diagnosticable, IEquatable<TextStyle>, ParagraphBuilder.ITextStyleProvider
    {
        public static readonly double _defaultFontSize = 14.0;
        public readonly bool inherit;
        public readonly Color color;
        public readonly double? fontSize;
        public readonly FontWeight? fontWeight;
        public readonly FontStyle? fontStyle;
        public readonly double? letterSpacing;
        public readonly double? wordSpacing;
        public readonly TextBaseline? textBaseline;
        public readonly double? height;
        public readonly TextDecoration decoration;
        public readonly Color decorationColor;
        public readonly TextDecorationStyle? decorationStyle;
        public readonly Paint background;
        public readonly string fontFamily;
        public readonly string debugLabel;

        const string _kDefaultDebugLabel = "unknown";


        public TextStyle(bool inherit = true, Color color = null, double? fontSize = null,
            FontWeight? fontWeight = null,
            FontStyle? fontStyle = null, double? letterSpacing = null, double? wordSpacing = null,
            TextBaseline? textBaseline = null, double? height = null, Paint background = null,
            TextDecoration decoration = null,
            Color decorationColor = null, TextDecorationStyle? decorationStyle = null,
            string fontFamily = null, string debugLabel = null)
        {
            this.inherit = inherit;
            this.color = color;
            this.fontSize = fontSize;
            this.fontWeight = fontWeight;
            this.fontStyle = fontStyle;
            this.letterSpacing = letterSpacing;
            this.wordSpacing = wordSpacing;
            this.textBaseline = textBaseline;
            this.height = height;
            this.decoration = decoration;
            this.decorationColor = decorationColor;
            this.decorationStyle = decorationStyle;
            this.fontFamily = fontFamily;
            this.debugLabel = debugLabel;
            this.background = background;
        }

        public  ui.TextStyle getTextStyle(ui.TextStyle currentStyle = null)
        {
            if (currentStyle != null)
            {
                return new ui.TextStyle(
                    color:  color??currentStyle.color,
                    fontSize: fontSize??currentStyle.fontSize,
                    fontWeight: fontWeight??currentStyle.fontWeight,
                    fontStyle: fontStyle??currentStyle.fontStyle,
                    letterSpacing: letterSpacing??currentStyle.letterSpacing,
                    wordSpacing: wordSpacing??currentStyle.wordSpacing,
                    textBaseline: textBaseline??currentStyle.textBaseline,
                    height: height??currentStyle.height,
                    decoration: decoration??currentStyle.decoration,
                    decorationColor: decorationColor??currentStyle.decorationColor,
                    fontFamily: fontFamily??currentStyle.fontFamily,
                    background: background??currentStyle.background
                );
            }
            
            return new ui.TextStyle(
                color:  color,
                fontSize: fontSize,
                fontWeight: fontWeight,
                fontStyle: fontStyle,
                letterSpacing: letterSpacing,
                wordSpacing: wordSpacing,
                textBaseline: textBaseline,
                height: height,
                decoration: decoration,
                decorationColor: decorationColor,
                fontFamily: fontFamily,
                background: background
            );
        }

        public RenderComparison compareTo(TextStyle other)
        {
            if (inherit != other.inherit || fontFamily != other.fontFamily
                                         || fontSize != other.fontSize || fontWeight != other.fontWeight
                                         || fontStyle != other.fontStyle || letterSpacing != other.letterSpacing
                                         || wordSpacing != other.wordSpacing || textBaseline != other.textBaseline
                                         || height != other.height || background != other.background)
            {
                return RenderComparison.layout;
            }

            if (color != other.color || decoration != other.decoration || decorationColor != other.decorationColor
                || decorationStyle != other.decorationStyle)
            {
                return RenderComparison.paint;
            }

            return RenderComparison.identical;
        }

        public ParagraphStyle getParagraphStyle(TextAlign textAlign,
            TextDirection textDirection, string ellipsis, int maxLines, double textScaleFactor = 1.0)
        {
            return new ParagraphStyle(
                textAlign, textDirection, fontWeight, fontStyle,
                maxLines, (fontSize ?? _defaultFontSize) * textScaleFactor,
                fontFamily, height, ellipsis
            );
        }

        public TextStyle merge(TextStyle other)
        {
            if (other == null)
            {
                return this;
            }

            if (!other.inherit)
            {
                return other;
            }

            string mergedDebugLabel = null;
            D.assert(() =>
            {
                if (other.debugLabel != null || debugLabel != null)
                {
                    mergedDebugLabel = string.Format("({0}).merge({1})", debugLabel ?? _kDefaultDebugLabel,
                        other.debugLabel ?? _kDefaultDebugLabel);
                }

                return true;
            });

            return copyWith(
                color: other.color,
                fontFamily: other.fontFamily,
                fontSize: other.fontSize,
                fontWeight: other.fontWeight,
                fontStyle: other.fontStyle,
                letterSpacing: other.letterSpacing,
                wordSpacing: other.wordSpacing,
                textBaseline: other.textBaseline,
                height: other.height,
                decoration: other.decoration,
                decorationColor: other.decorationColor,
                decorationStyle: other.decorationStyle,
                debugLabel: mergedDebugLabel
            );
        }

        public TextStyle copyWith(Color color,
            String fontFamily,
            double? fontSize,
            FontWeight? fontWeight,
            FontStyle? fontStyle,
            double? letterSpacing,
            double? wordSpacing,
            TextBaseline? textBaseline = null,
            double? height = null,
            Paint background = null,
            TextDecoration decoration = null,
            Color decorationColor = null,
            TextDecorationStyle? decorationStyle = null,
            string debugLabel = null)
        {
            string newDebugLabel = null;
            D.assert(() =>
            {
                if (this.debugLabel != null)
                {
                    newDebugLabel = debugLabel ?? string.Format("({0}).copyWith", this.debugLabel);
                }

                return true;
            });

            return new TextStyle(
                inherit: inherit,
                color: color ?? this.color,
                fontFamily: fontFamily ?? this.fontFamily,
                fontSize: fontSize ?? this.fontSize,
                fontWeight: fontWeight ?? this.fontWeight,
                fontStyle: fontStyle ?? this.fontStyle,
                letterSpacing: letterSpacing ?? this.letterSpacing,
                wordSpacing: wordSpacing ?? this.wordSpacing,
                textBaseline: textBaseline ?? this.textBaseline,
                height: height ?? this.height,
                decoration: decoration ?? this.decoration,
                decorationColor: decorationColor ?? this.decorationColor,
                decorationStyle: decorationStyle ?? this.decorationStyle,
                background: background ?? this.background,
                debugLabel: newDebugLabel
            );
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties)
        {
            base.debugFillProperties(properties);

            List<DiagnosticsNode> styles = new List<DiagnosticsNode>();
            styles.Add(new DiagnosticsProperty<Color>("color", color, defaultValue: Diagnostics.kNullDefaultValue));
            styles.Add(new StringProperty("family", fontFamily, defaultValue: Diagnostics.kNullDefaultValue, quoted: false));
            styles.Add(new DiagnosticsProperty<double?>("size", fontSize, defaultValue: Diagnostics.kNullDefaultValue));
            string weightDescription = "";
            if (fontWeight != null)
            {
                switch (fontWeight)
                {
                    case FontWeight.w400:
                        weightDescription = "400";
                        break;
                    case FontWeight.w700:
                        weightDescription = "700";
                        break;
                }
            }

            styles.Add(new DiagnosticsProperty<FontWeight?>(
                "weight",
                fontWeight,
                description: weightDescription,
                defaultValue: Diagnostics.kNullDefaultValue
            ));
            styles.Add(new EnumProperty<FontStyle?>("style", fontStyle, defaultValue: Diagnostics.kNullDefaultValue));
            styles.Add(new DiagnosticsProperty<double?>("letterSpacing", letterSpacing, defaultValue: Diagnostics.kNullDefaultValue));
            styles.Add(new DiagnosticsProperty<double?>("wordSpacing", wordSpacing, defaultValue: Diagnostics.kNullDefaultValue));
            styles.Add(new EnumProperty<TextBaseline?>("baseline", textBaseline, defaultValue: Diagnostics.kNullDefaultValue));
            styles.Add(new DiagnosticsProperty<double?>("height", height, defaultValue: Diagnostics.kNullDefaultValue));
            styles.Add(new StringProperty("background", background == null ? null : background.ToString(), defaultValue: Diagnostics.kNullDefaultValue, quoted: false));
            if (decoration != null)
            {
                List<string> decorationDescription = new List<string>();
                if (decorationStyle != null)
                {
                    decorationDescription.Add(decorationStyle.ToString());
                }
                
                styles.Add(new DiagnosticsProperty<Color>("decorationColor", decorationColor, defaultValue: Diagnostics.kNullDefaultValue, 
                    level: DiagnosticLevel.fine));
                if (decorationColor != null)
                {
                    decorationDescription.Add(decorationColor.ToString());
                }
                
                styles.Add(new DiagnosticsProperty<TextDecoration>("decoration", decoration, defaultValue: Diagnostics.kNullDefaultValue,
                    level: DiagnosticLevel.hidden));
                if (decoration != null)
                    decorationDescription.Add("$decoration");
                D.assert(decorationDescription.isNotEmpty);
                styles.Add(new MessageProperty("decoration", string.Join(" ", decorationDescription.ToArray())));
            }

            bool styleSpecified = styles.Any((DiagnosticsNode n) => !n.isFiltered(DiagnosticLevel.info));
            properties.add(new DiagnosticsProperty<bool>("inherit", inherit,
                level: (!styleSpecified && inherit) ? DiagnosticLevel.fine : DiagnosticLevel.info));
            foreach (var style in styles)
            {
                properties.add(style);
            }

            if (!styleSpecified)
                properties.add(new FlagProperty("inherit", value: inherit, ifTrue: "<all styles inherited>",
                    ifFalse: "<no style specified>"));
        }

        public bool Equals(TextStyle other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return inherit == other.inherit && Equals(color, other.color) && fontSize.Equals(other.fontSize) &&
                   fontWeight == other.fontWeight && fontStyle == other.fontStyle &&
                   letterSpacing.Equals(other.letterSpacing) && wordSpacing.Equals(other.wordSpacing) &&
                   textBaseline == other.textBaseline && height.Equals(other.height) &&
                   Equals(decoration, other.decoration) && Equals(decorationColor, other.decorationColor) &&
                   decorationStyle == other.decorationStyle && Equals(background, other.background) &&
                   string.Equals(fontFamily, other.fontFamily);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TextStyle) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = inherit.GetHashCode();
                hashCode = (hashCode * 397) ^ (color != null ? color.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ fontSize.GetHashCode();
                hashCode = (hashCode * 397) ^ fontWeight.GetHashCode();
                hashCode = (hashCode * 397) ^ fontStyle.GetHashCode();
                hashCode = (hashCode * 397) ^ letterSpacing.GetHashCode();
                hashCode = (hashCode * 397) ^ wordSpacing.GetHashCode();
                hashCode = (hashCode * 397) ^ textBaseline.GetHashCode();
                hashCode = (hashCode * 397) ^ height.GetHashCode();
                hashCode = (hashCode * 397) ^ (decoration != null ? decoration.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (decorationColor != null ? decorationColor.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ decorationStyle.GetHashCode();
                hashCode = (hashCode * 397) ^ (background != null ? background.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (fontFamily != null ? fontFamily.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(TextStyle left, TextStyle right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TextStyle left, TextStyle right)
        {
            return !Equals(left, right);
        }

        public override string toStringShort()
        {
            return this.GetType().FullName;
        }
    }
}