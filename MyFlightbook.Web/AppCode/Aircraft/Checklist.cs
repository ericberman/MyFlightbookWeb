﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

/******************************************************
 * 
 * Copyright (c) 2018-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Checklists
{

    public abstract class ChecklistRow
    {
        #region Properties
        /// <summary>
        /// What is the content of the row?
        /// </summary>
        public string Content { get; protected set; }

        /// <summary>
        /// For a challenge/response, what is the response?
        /// </summary>
        public string Response { get; protected set; }
        #endregion

        #region Constructors
        protected ChecklistRow(string content = "", string response = "")
        {
            Content = content;
            Response = response;
        }
        #endregion

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0} {1}", Content ?? string.Empty, Response ?? string.Empty);
        }

        private static Regex regexPrefix = null;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public static ChecklistRow ParseRowType(string szRow)
        {
            if (String.IsNullOrEmpty(szRow))
                throw new ArgumentNullException(nameof(szRow));

            if (regexPrefix == null)
                regexPrefix = new Regex("^(?<prefix>Skin|Tab|-{1,2}|\\*{1,2}|\\[(?: |[eEbB][+-])?])?(?<content>.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            MatchCollection mc = regexPrefix.Matches(szRow.Trim());

            if (mc == null || mc.Count == 0)
                return new ContentRow(szRow);

            GroupCollection gc = mc[0].Groups;
            if (gc == null || gc.Count == 0)
                return new ContentRow(szRow);

            string szPrefix = gc["prefix"].Value.Trim();
            string szContent = gc["content"].Value.Trim();
            string szResponse = string.Empty;

            if (String.IsNullOrWhiteSpace(szContent))
                return null;

            if (szContent.Contains("..."))
            {
                string[] rgsz = szContent.Split(new string[] { "..." }, StringSplitOptions.RemoveEmptyEntries);
                if (rgsz.Length == 2)
                {
                    szContent = rgsz[0].Trim();
                    szResponse = rgsz[1].Trim();
                }
            }

            if (szPrefix == null)
                return new ContentRow(szRow);

            switch (szPrefix.ToUpper(CultureInfo.CurrentCulture))
            {
                default:
                case "":
                    return new ContentRow(szContent);
                case "SKIN":
                    return new SkinRow(szContent);
                case "TAB":
                    return new TabContainer(szContent);
                case "-":
                    return new HeaderContainer(szContent);
                case "--":
                    return new SubHeaderContainer(szContent);
                case "*":
                    return new ContentRow(szContent) { Style = ContentStyle.Emphasis };
                case "**":
                    return new ContentRow(szContent) { Style = ContentStyle.Emergency };
                case "[]":
                case "[ ]":
                    return new CheckboxRow(szContent, szResponse);
                case "[E+]":
                    return new CheckboxRow(szContent, szResponse, CheckboxAction.StartEngine);
                case "[E-]":
                    return new CheckboxRow(szContent, szResponse, CheckboxAction.StopEngine);
                case "[B+]":
                    return new CheckboxRow(szContent, szResponse, CheckboxAction.BlockOut);
                case "[B-]":
                    return new CheckboxRow(szContent, szResponse, CheckboxAction.BlockIn);
            }
        }
    }

    public enum CheckboxAction { None, StartEngine, StopEngine, BlockOut, BlockIn }

    public enum ContentStyle { Normal, Emphasis, Emergency }

    public class ContentRow : ChecklistRow
    {
        public ContentStyle Style { get; set; }

        public ContentRow(string szContent = "") : base(szContent)
        {
            Style = ContentStyle.Normal;
        }
    }

    public class SkinRow : ChecklistRow
    {
        public SkinRow(string szContent = "") : base(szContent) { }
    }

    public class CheckboxRow : ContentRow
    {
        #region Properties
        /// <summary>
        /// Any side effect from completing this action?
        /// </summary>
        public CheckboxAction Action { get; protected set; }
        #endregion

        public CheckboxRow(string content, string response = null, CheckboxAction checkboxAction = CheckboxAction.None) : base(content)
        {
            Response = response;
            Action = checkboxAction;
        }

        public string ActionForRow
        {
            get
            {
                switch (Action)
                {
                    default:
                    case CheckboxAction.None:
                        return string.Empty;
                    case CheckboxAction.StartEngine:
                        return "startEngine";
                    case CheckboxAction.StopEngine:
                        return "stopEngine";
                    case CheckboxAction.BlockOut:
                        return "blockOut";
                    case CheckboxAction.BlockIn:
                        return "blockIn";
                }
            }
        }
    }

    #region Container rows
    public abstract class ContainerRow : ContentRow
    {
        private readonly List<ChecklistRow> m_containedItems;

        #region properties
        /// <summary>
        /// Sub-items of this row - get only
        /// </summary>
        public IEnumerable<ChecklistRow> ContainedItems { get { return m_containedItems; } }

        /// <summary>
        /// Container level - i.e., does this go into the active container, next to it, or start a new active container?
        /// </summary>
        public int Level { get; protected set; }
        #endregion

        public void AddItem(ChecklistRow ckl)
        {
            m_containedItems.Add(ckl);
        }

        #region Constructors
        protected ContainerRow(int level = 0, string content = "") : base()
        {
            Content = content;
            Level = level;
            m_containedItems = new List<ChecklistRow>();
        }
        #endregion
    }

    #region Concrete containers: tabs and headers
    public class RootContainer : ContainerRow
    {
        public RootContainer() : base(0) { }
    }

    public class TabContainer : ContainerRow
    {
        public TabContainer(string content = "") : base(1, content) { }
    }

    public class HeaderContainer : ContainerRow
    {
        public HeaderContainer(string content) : base(2, content) { }
    }

    public class SubHeaderContainer : ContainerRow
    {
        public SubHeaderContainer(string content) : base(3, content) { }
    }
    #endregion // concrete containers
    #endregion // Containers

    public class Checklist
    {
        private readonly ContainerRow m_rootContainer;

        public static string CssClassForContentStyle(ContentStyle style)
        {
            switch (style)
            {
                default:
                case ContentStyle.Normal:
                    return "checklistContentStyleNormal";
                case ContentStyle.Emphasis:
                    return "checklistContentStyleEmphasis";
                case ContentStyle.Emergency:
                    return "checklistContentStyleEmergency";
            }
        }

        #region Properties
        public ContainerRow Root { get { return m_rootContainer; } }
        #endregion

        #region constructors
        public Checklist()
        {
        }

        public Checklist(string input) : this()
        {
            Stack<ContainerRow> containers = new Stack<ContainerRow>();
            containers.Push(m_rootContainer = new RootContainer());
            using (StringReader sr = new StringReader(input))
            {
                string szItem = null;
                while ((szItem = sr.ReadLine()) != null)
                {
                    if (String.IsNullOrWhiteSpace(szItem))
                        continue;

                    ChecklistRow checklistRow = ChecklistRow.ParseRowType(szItem);

                    if (checklistRow == null)
                        continue;

                    if (checklistRow is SkinRow)
                    {
                        // TODO: apply the skin, no actual content in a skin row.
                        continue;
                    }

                    if (checklistRow is RootContainer)
                        throw new InvalidDataException("Can only have one root container in Checklist");


                    if (checklistRow is ContainerRow container)
                    {
                        int level = container.Level;

                        while (containers.Count > 0 && containers.Peek().Level >= level)
                            containers.Pop();

                        if (containers.Count == 0)
                            throw new InvalidDataException("No root container found in checklist");

                        containers.Peek().AddItem(container);
                        containers.Push(container);
                    }
                    else
                        containers.Peek().AddItem(checklistRow);
                }
            }
        }
        #endregion
    }
}
