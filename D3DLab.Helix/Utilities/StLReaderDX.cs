﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using SharpDX;

namespace HelixToolkit.Wpf.SharpDX
{

	/// <summary>
	/// Provides an importer for StereoLithography .StL files.
	/// </summary>
	/// <remarks>
	/// The format is documented on <a href="http://en.wikipedia.org/wiki/STL_(file_format)">Wikipedia</a>.
	/// </remarks>
	public class StLReaderDX
	{
		private PhongMaterial DefaultMaterial = new PhongMaterial()
		{
			DiffuseColor = Color4.White
		};

		private readonly Dispatcher dispatcher;
		/// <summary>
		/// The regular expression used to parse normal vectors.
		/// </summary>
		private static readonly Regex NormalRegex = new Regex(@"normal\s*(\S*)\s*(\S*)\s*(\S*)", RegexOptions.Compiled);

		/// <summary>
		/// The regular expression used to parse vertices.
		/// </summary>
		private static readonly Regex VertexRegex = new Regex(@"vertex\s*(\S*)\s*(\S*)\s*(\S*)", RegexOptions.Compiled);

		/// <summary>
		/// The index.
		/// </summary>
		private int index;

		/// <summary>
		/// The last color.
		/// </summary>
		private Color lastColor;

		/// <summary>
		/// Initializes a new instance of the <see cref="StLReader" /> class.
		/// </summary>
		/// <param name="dispatcher">The dispatcher.</param>
		public StLReaderDX(Dispatcher dispatcher = null)
		{
			this.dispatcher = dispatcher ?? System.Windows.Application.Current.Dispatcher;
			this.Meshes = new List<MeshBuilder>();
			this.Materials = new List<PhongMaterial>();
		}

		/// <summary>
		/// Gets the file header.
		/// </summary>
		/// <value>
		/// The header.
		/// </value>
		public string Header { get; private set; }

		/// <summary>
		/// Gets the materials.
		/// </summary>
		/// <value> The materials. </value>
		public IList<PhongMaterial> Materials { get; private set; }

		/// <summary>
		/// Gets the meshes.
		/// </summary>
		/// <value> The meshes. </value>
		public IList<MeshBuilder> Meshes { get; private set; }

		/// <summary>
		/// Reads the model from the specified stream.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <returns>The model.</returns>
		public CompositeModel3D Read(Stream stream)
		{
			// Try to read in BINARY format
			var success = this.TryReadBinary(stream);
			if (!success)
			{
				// Reset position of stream
				stream.Position = 0;

				// Read in ASCII format
				success = this.TryReadAscii(stream);
			}

			if (success)
			{
				return this.ToModel3D();
			}

			return null;
		}

		/// <summary>
		/// Builds the model.
		/// </summary>
		/// <returns>The model.</returns>
		public CompositeModel3D ToModel3D()
		{
			CompositeModel3D modelGroup = dispatcher != null
				? dispatcher.Invoke(new Func<CompositeModel3D>(BuildModel))
				: BuildModel();
			return modelGroup;
		}

		private CompositeModel3D BuildModel()
		{
			var modelGroup = new CompositeModel3D();
			int i = 0;
			foreach (var mesh in this.Meshes)
			{
				var gm = new MeshGeometryModel3D
							 {
								 Geometry = mesh.ToMeshGeometry3D(),
								 Material = this.Materials[i],
							 };

				modelGroup.Children.Add(gm);
				i++;
			}
			return modelGroup;
		}
		/// <summary>
		/// Parses the ID and values from the specified line.
		/// </summary>
		/// <param name="line">
		/// The line.
		/// </param>
		/// <param name="id">
		/// The id.
		/// </param>
		/// <param name="values">
		/// The values.
		/// </param>
		private static void ParseLine(string line, out string id, out string values)
		{
			line = line.Trim();
			int idx = line.IndexOf(' ');
			if (idx == -1)
			{
				id = line;
				values = string.Empty;
			}
			else
			{
				id = line.Substring(0, idx).ToLower();
				values = line.Substring(idx + 1);
			}
		}

		/// <summary>
		/// Parses a normal string.
		/// </summary>
		/// <param name="input">
		/// The input string.
		/// </param>
		/// <returns>
		/// The normal vector.
		/// </returns>
		private static Vector3 ParseNormal(string input)
		{
			var match = NormalRegex.Match(input);
			if (!match.Success)
				throw new FileFormatException("Unexpected line.");

			var x = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
			var y = float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
			var z = float.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

			return new Vector3(x, y, z);
		}

		/// <summary>
		/// Reads a float (4 byte)
		/// </summary>
		/// <param name="reader">
		/// The reader.
		/// </param>
		/// <returns>
		/// The float.
		/// </returns>
		private static float ReadFloat(BinaryReader reader)
		{
			var bytes = reader.ReadBytes(4);
			return BitConverter.ToSingle(bytes, 0);
		}

		/// <summary>
		/// Reads a line from the stream reader.
		/// </summary>
		/// <param name="reader">
		/// The stream reader.
		/// </param>
		/// <param name="token">
		/// The expected token ID.
		/// </param>
		/// <exception cref="FileFormatException">
		/// The expected token ID was not matched.
		/// </exception>
		private static void ReadLine(StreamReader reader, string token)
		{
			if (token == null)
			{
				throw new ArgumentNullException("token");
			}

			var line = reader.ReadLine();
			string id, values;
			ParseLine(line, out id, out values);

			if (!string.Equals(token, id, StringComparison.OrdinalIgnoreCase))
			{
				throw new FileFormatException("Unexpected line.");
			}
		}

		/// <summary>
		/// Reads a 16-bit unsigned integer.
		/// </summary>
		/// <param name="reader">
		/// The reader.
		/// </param>
		/// <returns>
		/// The unsigned integer.
		/// </returns>
		private static ushort ReadUInt16(BinaryReader reader)
		{
			var bytes = reader.ReadBytes(2);
			return BitConverter.ToUInt16(bytes, 0);
		}

		/// <summary>
		/// Reads a 32-bit unsigned integer.
		/// </summary>
		/// <param name="reader">
		/// The reader.
		/// </param>
		/// <returns>
		/// The unsigned integer.
		/// </returns>
		private static uint ReadUInt32(BinaryReader reader)
		{
			var bytes = reader.ReadBytes(4);
			return BitConverter.ToUInt32(bytes, 0);
		}

		/// <summary>
		/// Tries to parse a vertex from a string.
		/// </summary>
		/// <param name="line">
		/// The input string.
		/// </param>
		/// <param name="point">
		/// The vertex point.
		/// </param>
		/// <returns>
		/// True if parsing was successful.
		/// </returns>
		private static bool TryParseVertex(string line, out Vector3 point)
		{
			var match = VertexRegex.Match(line);
			if (!match.Success)
			{
				point = new Vector3();
				return false;
			}

			float x = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
			float y = float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
			float z = float.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

			point = new Vector3(x, y, z);
			return true;
		}

		/// <summary>
		/// Reads a facet.
		/// </summary>
		/// <param name="reader">
		/// The stream reader.
		/// </param>
		/// <param name="normal">
		/// The normal. 
		/// </param>
		private void ReadFacet(StreamReader reader, string normal)
		{
#pragma warning disable 168
			var n = ParseNormal(normal);
#pragma warning restore 168
			var points = new List<Vector3>();
			ReadLine(reader, "outer");
			while (true)
			{
				var line = reader.ReadLine();
				Vector3 point;
				if (TryParseVertex(line, out point))
				{
					points.Add(point);
					continue;
				}

				string id, values;
				ParseLine(line, out id, out values);

				if (id == "endloop")
				{
					break;
				}
			}

			ReadLine(reader, "endfacet");

			if (this.Materials.Count < this.index + 1)
			{
				this.Materials.Add(this.DefaultMaterial);
			}

			if (this.Meshes.Count < this.index + 1)
			{
				this.Meshes.Add(new MeshBuilder(true, true));
			}

			this.Meshes[this.index].AddPolygon(points);

			// todo: add normals
		}

		/// <summary>
		/// Reads a triangle from a binary STL file.
		/// </summary>
		/// <param name="reader">
		/// The reader.
		/// </param>
		private void ReadTriangle(BinaryReader reader)
		{
			float ni = ReadFloat(reader);
			float nj = ReadFloat(reader);
			float nk = ReadFloat(reader);

#pragma warning disable 168
			var n = new Vector3(ni, nj, nk);
#pragma warning restore 168

			float x1 = ReadFloat(reader);
			float y1 = ReadFloat(reader);
			float z1 = ReadFloat(reader);
			var v1 = new Vector3(x1, y1, z1);

			float x2 = ReadFloat(reader);
			float y2 = ReadFloat(reader);
			float z2 = ReadFloat(reader);
			var v2 = new Vector3(x2, y2, z2);

			float x3 = ReadFloat(reader);
			float y3 = ReadFloat(reader);
			float z3 = ReadFloat(reader);
			var v3 = new Vector3(x3, y3, z3);

			var attrib = Convert.ToString(ReadUInt16(reader), 2).PadLeft(16, '0').ToCharArray();
			var hasColor = attrib[0].Equals('1');

			if (hasColor)
			{
				int blue = attrib[15].Equals('1') ? 1 : 0;
				blue = attrib[14].Equals('1') ? blue + 2 : blue;
				blue = attrib[13].Equals('1') ? blue + 4 : blue;
				blue = attrib[12].Equals('1') ? blue + 8 : blue;
				blue = attrib[11].Equals('1') ? blue + 16 : blue;
				int b = blue * 8;

				int green = attrib[10].Equals('1') ? 1 : 0;
				green = attrib[9].Equals('1') ? green + 2 : green;
				green = attrib[8].Equals('1') ? green + 4 : green;
				green = attrib[7].Equals('1') ? green + 8 : green;
				green = attrib[6].Equals('1') ? green + 16 : green;
				int g = green * 8;

				int red = attrib[5].Equals('1') ? 1 : 0;
				red = attrib[4].Equals('1') ? red + 2 : red;
				red = attrib[3].Equals('1') ? red + 4 : red;
				red = attrib[2].Equals('1') ? red + 8 : red;
				red = attrib[1].Equals('1') ? red + 16 : red;
				int r = red * 8;

				var currentColor = new Color(Convert.ToByte(r), Convert.ToByte(g), Convert.ToByte(b));

				if (!Color.Equals(this.lastColor, currentColor))
				{
					this.lastColor = currentColor;
					this.index++;
				}

				if (this.Materials.Count < this.index + 1)
				{
					this.Materials.Add(new PhongMaterial()
					{
						DiffuseColor = currentColor
					});
				}
			}
			else
			{
				if (this.Materials.Count < this.index + 1)
				{
					this.Materials.Add(this.DefaultMaterial);
				}
			}

			if (this.Meshes.Count < this.index + 1)
			{
				this.Meshes.Add(new MeshBuilder(true, true));
			}

			this.Meshes[this.index].AddTriangle(v1, v2, v3);

			// todo: add normal
		}

		/// <summary>
		/// Reads the model in ASCII format from the specified stream.
		/// </summary>
		/// <param name="stream">
		/// The stream.
		/// </param>
		/// <returns>
		/// True if the model was loaded successfully.
		/// </returns>
		private bool TryReadAscii(Stream stream)
		{
			var reader = new StreamReader(stream);
			this.Meshes.Add(new MeshBuilder(true, true));
			this.Materials.Add(this.DefaultMaterial);

			while (!reader.EndOfStream)
			{
				var line = reader.ReadLine();
				if (line == null)
				{
					continue;
				}

				line = line.Trim();
				if (line.Length == 0 || line.StartsWith("\0") || line.StartsWith("#") || line.StartsWith("!")
					|| line.StartsWith("$"))
				{
					continue;
				}

				string id, values;
				ParseLine(line, out id, out values);
				switch (id)
				{
					case "solid":
						this.Header = values.Trim();
						break;
					case "facet":
						this.ReadFacet(reader, values);
						break;
					case "endsolid":
						break;
				}
			}

			return true;
		}

		/// <summary>
		/// Reads the model from the specified binary stream.
		/// </summary>
		/// <param name="stream">
		/// The stream.
		/// </param>
		/// <returns>
		/// True if the file was read successfully.
		/// </returns>
		/// <exception cref="System.IO.FileFormatException">
		/// Incomplete file
		/// </exception>
		private bool TryReadBinary(Stream stream)
		{
			long length = stream.Length;
			if (length < 84)
			{
				throw new FileFormatException("Incomplete file");
			}

			var reader = new BinaryReader(stream);
			this.Header = System.Text.Encoding.ASCII.GetString(reader.ReadBytes(80)).Trim();
			uint numberTriangles = ReadUInt32(reader);

			if (length - 84 != numberTriangles * 50)
			{
				return false;
			}

			this.index = 0;
			this.Meshes.Add(new MeshBuilder(true, true));
			this.Materials.Add(this.DefaultMaterial);

			for (int i = 0; i < numberTriangles; i++)
			{
				this.ReadTriangle(reader);
			}

			return true;
		}
	}
}