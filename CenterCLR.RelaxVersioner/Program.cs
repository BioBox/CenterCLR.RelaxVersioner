﻿/////////////////////////////////////////////////////////////////////////////////////////////////
//
// CenterCLR.RelaxVersioner - Easy-usage, Git-based, auto-generate version informations toolset.
// Copyright (c) 2016 Kouji Matsui (@kekyo2)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//	http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
/////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using LibGit2Sharp;

using CenterCLR.RelaxVersioner.Writers;

namespace CenterCLR.RelaxVersioner
{
	public static class Program
	{
		private static readonly Dictionary<string, WriterBase> writers_;

		static Program()
		{
			writers_ = typeof(Program).Assembly.
				GetTypes().
				Where(type => type.IsSealed && type.IsClass && typeof(WriterBase).IsAssignableFrom(type)).
				Select(type => (WriterBase)Activator.CreateInstance(type)).
				ToDictionary(writer => writer.Language, StringComparer.InvariantCultureIgnoreCase);
		}

		public static int Main(string[] args)
		{
			try
			{
				var solutionDirectory = args[0];
				var projectDirectory = args[1];
				var targetPath = args[2];
				var targetFrameworkVersion = args[3];
				var language = args[4];

				var writer = writers_[language];

				var ruleSets = Utilities.AggregateRuleSets(
					Utilities.LoadRuleSet(projectDirectory),
					Utilities.LoadRuleSet(solutionDirectory),
					Utilities.GetDefaultRuleSet());

				var ruleSet = ruleSets[language];

				using (var repository = new Repository(solutionDirectory))
				{
					var tags = repository.Tags.
						Where(tag => tag.Target is Commit).
						GroupBy(tag => tag.Target.Sha).
						ToDictionary(g => g.Key, g => g.ToList().AsEnumerable());

					writer.Write(
						targetPath,
						repository.Head,
						tags,
						targetFrameworkVersion == "v4.0",
						DateTime.UtcNow,
						ruleSet);

					Console.WriteLine("RelaxVersioner: Generated versions code: Language={0}", language);
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine("RelaxVersioner: " + ex.Message);
				return Marshal.GetHRForException(ex);
			}

			return 0;
		}
	}
}
