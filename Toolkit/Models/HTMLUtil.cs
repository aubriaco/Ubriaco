/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Solusek Systems. All rights reserved.
 *--------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ubriaco.Toolkit;

internal static class HTMLUtil
{

    public static void ProcessMatches(ref string body, IEnumerable<Match> matches)
    {
        foreach (Match m in matches)
        {

            if (!string.IsNullOrWhiteSpace(m.Groups[1].Value))
            {
                if (!m.Groups[1].Value.Contains('{') && !m.Groups[1].Value.Contains('>'))
                {
                    var newValue = " {{getLang('" + m.Groups[1].Value.Trim().Replace("'", "\\'") + "')}} ";
                    body = body.Replace(">" + m.Groups[1].Value + "<", ">" + newValue + "<");
                }
            }
        }
    }

    public static void ProcessMatchesRawHTML(ref string body, IEnumerable<Match> matches)
    {
        foreach (Match m in matches)
        {
            string cont = m.Groups[1].Value;
            string post = m.Groups[2].Value;
            if (!string.IsNullOrWhiteSpace(m.Groups[1].Value))
            {
                if (!cont.Contains('>')
                    && !post.ToLower().Contains("title")
                    && !post.ToLower().Contains("script")
                     && !post.ToLower().Contains("style"))
                {
                    var newValue = "<span class=\"lang\">" + cont.Trim() + "</span>";
                    body = body.Replace(">" + cont + "<", ">" + newValue + "<");
                }
            }
        }
    }

    public static void ProcessMatchesReactJS(ref string body, IEnumerable<Match> matches)
    {
        foreach (Match m in matches)
        {

            if (!string.IsNullOrWhiteSpace(m.Groups[1].Value))
            {
                if (!m.Groups[1].Value.Contains('{')
                && !m.Groups[1].Value.Contains('}')
                    && !m.Groups[1].Value.Contains('>')
                    && !m.Groups[1].Value.Contains('<')
                    && !m.Groups[1].Value.Contains('(')
                    && !m.Groups[1].Value.Contains(')')
                    && !m.Groups[1].Value.Contains(':')
                    && m.Groups[1].Value.Trim() != ","
                    && !m.Groups[1].Value.Contains(" ? ")
                    && !m.Groups[1].Value.Contains("&&")
                    && !m.Groups[1].Value.Contains("return"))
                {
                    var newValue = "<L>" + m.Groups[1].Value.Trim() + "</L>";
                    body = body.Replace(">" + m.Groups[1].Value + "<", ">" + newValue + "<");
                }
            }
        }
    }
}
