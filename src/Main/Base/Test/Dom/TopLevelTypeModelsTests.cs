﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.SharpDevelop.Parser;
using ICSharpCode.SharpDevelop.Project;
using NUnit.Framework;
using Rhino.Mocks;
namespace ICSharpCode.SharpDevelop.Dom
{
	class TopLevelTypeModelsTests : CSharpModelTestBase
	{
		protected List<RemovedAndAddedPair<ITypeDefinitionModel>> topLevelChangeEventArgs;
		protected List<RemovedAndAddedPair<INamespaceModel>> namespaceChangeEventArgs;
		
		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			topLevelChangeEventArgs = ListenForChanges(assemblyModel.TopLevelTypeDefinitions);
			namespaceChangeEventArgs = ListenForChanges(assemblyModel.Namespaces);
		}
		
		[Test]
		public void EmptyProject()
		{
			Assert.AreEqual(0, assemblyModel.TopLevelTypeDefinitions.Count);
			Assert.IsNull(assemblyModel.TopLevelTypeDefinitions[new TopLevelTypeName("MissingClass")]);
		}
		
		#region Simple class
		[Test]
		public void AddSimpleClass()
		{
			AddCodeFile("test.cs", @"class SimpleClass {}");
			Assert.AreEqual(1, assemblyModel.TopLevelTypeDefinitions.Count);
			var simpleClass = assemblyModel.TopLevelTypeDefinitions.Single();
			Assert.IsEmpty(topLevelChangeEventArgs.Single().OldItems);
			Assert.AreEqual(new[] {
			                	simpleClass
			                }, topLevelChangeEventArgs.Single().NewItems);
			topLevelChangeEventArgs.Clear();
			// clear for follow-up tests
		}
		[Test]
		public void UpdateSimpleClass()
		{
			AddSimpleClass();
			var simpleClass = assemblyModel.TopLevelTypeDefinitions.Single();
			Assert.AreEqual(0, simpleClass.Members.Count);
			UpdateCodeFile("test.cs", "class SimpleClass { void Method() {} }");
			Assert.AreSame(simpleClass, assemblyModel.TopLevelTypeDefinitions.Single());
			Assert.AreEqual(1, simpleClass.Members.Count);
			Assert.IsEmpty(topLevelChangeEventArgs);
		}
		[Test]
		public void ReplaceSimpleClass()
		{
			AddSimpleClass();
			var simpleClass = assemblyModel.TopLevelTypeDefinitions.Single();
			UpdateCodeFile("test.cs", "class OtherClass { }");
			var otherClass = assemblyModel.TopLevelTypeDefinitions.Single();
			Assert.AreNotSame(simpleClass, otherClass);
			Assert.AreEqual(new[] {
			                	simpleClass
			                }, topLevelChangeEventArgs.Single().OldItems);
			Assert.AreEqual(new[] {
			                	otherClass
			                }, topLevelChangeEventArgs.Single().NewItems);
		}
		[Test]
		public void RemoveSimpleClassCodeFile()
		{
			AddSimpleClass();
			var simpleClass = assemblyModel.TopLevelTypeDefinitions.Single();
			RemoveCodeFile("test.cs");
			Assert.IsEmpty(assemblyModel.TopLevelTypeDefinitions);
			// check removal event
			Assert.AreEqual(new[] {
			                	simpleClass
			                }, topLevelChangeEventArgs.Single().OldItems);
			Assert.IsEmpty(topLevelChangeEventArgs.Single().NewItems);
			// test that accessing properties of a removed class still works
			Assert.AreEqual(new FullTypeName("SimpleClass"), simpleClass.FullTypeName);
		}
		#endregion
		
		#region TwoPartsInSingleFile
		[Test]
		public void TwoPartsInSingleFile()
		{
			AddCodeFile("test.cs", @"
partial class SimpleClass { void Member1() {} }
partial class SimpleClass { void Member2() {} }");
			Assert.AreEqual(1, assemblyModel.TopLevelTypeDefinitions.Count);
			topLevelChangeEventArgs.Clear();
			// clear for follow-up tests
		}
		[Test]
		public void UpdateTwoPartsInSingleFile()
		{
			TwoPartsInSingleFile();
			var simpleClass = assemblyModel.TopLevelTypeDefinitions.Single();
			UpdateCodeFile("test.cs", @"
partial class SimpleClass { void Member1b() {} }
partial class SimpleClass { void Member2b() {} }");
			Assert.AreSame(simpleClass, assemblyModel.TopLevelTypeDefinitions.Single());
			Assert.AreEqual(new[] {
			                	"Member1b",
			                	"Member2b"
			                }, simpleClass.Members.Select(m => m.Name).ToArray());
		}
		[Test]
		public void RemoveOneOfPartsInSingleFile()
		{
			TwoPartsInSingleFile();
			var simpleClass = assemblyModel.TopLevelTypeDefinitions.Single();
			var member1 = simpleClass.Members.First();
			UpdateCodeFile("test.cs", "class SimpleClass { void Member1() {} }");
			Assert.AreSame(simpleClass, assemblyModel.TopLevelTypeDefinitions.Single());
			Assert.AreSame(member1, simpleClass.Members.Single());
		}
		[Test]
		public void RemoveBothPartsInSingleFile()
		{
			TwoPartsInSingleFile();
			var simpleClass = assemblyModel.TopLevelTypeDefinitions.Single();
			UpdateCodeFile("test.cs", "class OtherClass {}");
			Assert.AreNotSame(simpleClass, assemblyModel.TopLevelTypeDefinitions.Single());
		}
		#endregion
		
		#region TestRegionIsFromPrimaryPart
		[Test]
		public void AddingDesignerPartDoesNotChangeRegion()
		{
			AddCodeFile("Form.cs", "partial class MyForm {}");
			Assert.AreEqual("Form.cs", assemblyModel.TopLevelTypeDefinitions.Single().Region.FileName);
			AddCodeFile("Form.Designer.cs", "partial class MyForm {}");
			Assert.AreEqual("Form.cs", assemblyModel.TopLevelTypeDefinitions.Single().Region.FileName);
		}
		[Test]
		public void AddingPrimaryPartChangesRegion()
		{
			AddCodeFile("Form.Designer.cs", "partial class MyForm {}");
			Assert.AreEqual("Form.Designer.cs", assemblyModel.TopLevelTypeDefinitions.Single().Region.FileName);
			AddCodeFile("Form.cs", "partial class MyForm {}");
			Assert.AreEqual("Form.cs", assemblyModel.TopLevelTypeDefinitions.Single().Region.FileName);
		}
		[Test]
		public void RemovingPrimaryPartChangesRegionToNextBestPart()
		{
			AddCodeFile("Form.cs", "partial class MyForm { int primaryMember; }");
			AddCodeFile("Form.Designer.Designer.cs", "partial class MyForm { int designer2; }");
			var form = assemblyModel.TopLevelTypeDefinitions.Single();
			Assert.AreEqual(new[] {
			                	"primaryMember",
			                	"designer2"
			                }, form.Members.Select(m => m.Name).ToArray());
			AddCodeFile("Form.Designer.cs", "partial class MyForm { int designer; }");
			Assert.AreEqual("Form.cs", form.Region.FileName);
			Assert.AreEqual(new[] {
			                	"primaryMember",
			                	"designer",
			                	"designer2"
			                }, form.Members.Select(m => m.Name).ToArray());
			RemoveCodeFile("Form.cs");
			Assert.AreSame(form, assemblyModel.TopLevelTypeDefinitions.Single());
			Assert.AreEqual("Form.Designer.cs", form.Region.FileName);
			Assert.AreEqual(new[] {
			                	"designer",
			                	"designer2"
			                }, form.Members.Select(m => m.Name).ToArray());
		}
		#endregion
	}
}


