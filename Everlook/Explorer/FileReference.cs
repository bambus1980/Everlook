﻿//
//  FileReference.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2016 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using Warcraft.Core;
using System.IO;
using System.Text.RegularExpressions;
using Everlook.Package;
using Warcraft.MPQ.FileInfo;

namespace Everlook.Explorer
{
	/// <summary>
	/// Represents a file stored in a game package. Holds the package name and path of the file
	/// inside the package.
	/// </summary>
	public class FileReference : IEquatable<FileReference>
	{
		/// <summary>
		/// Gets or sets the group this reference belongs to.
		/// </summary>
		/// <value>The group.</value>
		public PackageGroup PackageGroup { get; protected set; }

		/// <summary>
		/// Gets or sets the name of the package where the file is stored.
		/// </summary>
		/// <value>The name of the package.</value>
		public virtual string PackageName { get; set; } = "";

		/// <summary>
		/// Gets or sets the file path of the file inside the package.
		/// </summary>
		/// <value>The file path.</value>
		public virtual string FilePath { get; set; } = "";

		/// <summary>
		/// Gets the file info of this reference.
		/// </summary>
		/// <value>The file info.</value>
		public virtual MPQFileInfo ReferenceInfo
		{
			get
			{
				if (this.IsFile)
				{
					return this.PackageGroup.GetReferenceInfo(this);
				}
				else
				{
					return null;
				}
			}
		}

		/// <summary>
		/// Gets a value indicating whether this reference is deleted in package it is stored in.
		/// </summary>
		/// <value><c>true</c> if this instance is deleted in package; otherwise, <c>false</c>.</value>
		public virtual bool IsDeletedInPackage
		{
			get
			{
				if (this.ReferenceInfo != null && this.IsFile)
				{
					return this.ReferenceInfo.IsDeleted;
				}
				else if (this.IsDirectory)
				{
					return false;
				}

				return false;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this or not this reference is a package reference.
		/// </summary>
		/// <value><c>true</c> if this reference is a package; otherwise, <c>false</c>.</value>
		public virtual bool IsPackage => !string.IsNullOrEmpty(this.PackageName) && string.IsNullOrEmpty(this.FilePath);

		/// <summary>
		/// Gets a value indicating whether this reference is a directory.
		/// </summary>
		/// <value><c>true</c> if this instance is directory; otherwise, <c>false</c>.</value>
		public bool IsDirectory => !string.IsNullOrEmpty(this.FilePath) && GetReferencedFileType() == WarcraftFileType.Directory;

		/// <summary>
		/// Gets a value indicating whether this reference is a file.
		/// </summary>
		/// <value><c>true</c> if this instance is file; otherwise, <c>false</c>.</value>
		public bool IsFile => !string.IsNullOrEmpty(this.FilePath) && (GetReferencedFileType() != WarcraftFileType.Directory);

		/// <summary>
		/// The name of the file.
		/// </summary>
		public string Filename => Path.GetFileName(this.FilePath.Replace('\\', Path.DirectorySeparatorChar));

		/// <summary>
		/// Initializes a new instance of the <see cref="FileReference"/> class.
		/// This creates a new, empty item reference.
		/// </summary>
		protected FileReference()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="FileReference"/> class.
		/// </summary>
		/// <param name="inPackageGroup">The package group this reference belongs to.</param>
		/// <param name="inParentReference">The parent of this item reference.</param>
		/// <param name="inPackageName">The name of the package this reference belongs to.</param>
		/// <param name="inFilePath">The complete file path this reference points to.</param>
		public FileReference(PackageGroup inPackageGroup, string inPackageName, string inFilePath)
			: this(inPackageGroup)
		{
			this.PackageName = inPackageName;
			this.FilePath = inFilePath;
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="FileReference"/> class.
		/// </summary>
		/// <param name="inPackageGroup">PackageGroup.</param>
		public FileReference(PackageGroup inPackageGroup)
		{
			this.PackageGroup = inPackageGroup;
		}

		/// <summary>
		/// Extracts this instance from the package group it is associated with.
		/// </summary>
		public virtual byte[] Extract()
		{
			return this.PackageGroup.ExtractUnversionedReference(this);
		}

		/// <summary>
		/// Gets the type of the referenced file.
		/// </summary>
		/// <returns>The referenced file type.</returns>
		public WarcraftFileType GetReferencedFileType()
		{
			string itemPath = this.FilePath.ToLower();
			if (!itemPath.EndsWith("\\"))
			{
				string fileExtension = Path.GetExtension(itemPath).Replace(".", "");

				switch (fileExtension)
				{
					case "mpq":
					{
						return WarcraftFileType.MoPaQArchive;
					}
					case "toc":
					{
						return WarcraftFileType.AddonManifest;
					}
					case "sig":
					{
						return WarcraftFileType.AddonManifestSignature;
					}
					case "wtf":
					{
						return WarcraftFileType.ConfigurationFile;
					}
					case "dbc":
					{
						return WarcraftFileType.DatabaseContainer;
					}
					case "bls":
					{
						return WarcraftFileType.Shader;
					}
					case "wlw":
					{
						return WarcraftFileType.TerrainWater;
					}
					case "wlq":
					{
						return WarcraftFileType.TerrainLiquid;
					}
					case "wdl":
					{
						return WarcraftFileType.TerrainLiquid;
					}
					case "wdt":
					{
						return WarcraftFileType.TerrainTable;
					}
					case "adt":
					{
						return WarcraftFileType.TerrainData;
					}
					case "blp":
					{
						return WarcraftFileType.BinaryImage;
					}
					case "trs":
					{
						return WarcraftFileType.Hashmap;
					}
					case "m2":
					case "mdx":
					{
						return WarcraftFileType.GameObjectModel;
					}
					case "wmo":
					{
						Regex groupDetectRegex = new Regex("(.+_[0-9]{3}.wmo)", RegexOptions.Multiline);

						if (groupDetectRegex.IsMatch(itemPath))
						{
							return WarcraftFileType.WorldObjectModelGroup;
						}
						else
						{
							return WarcraftFileType.WorldObjectModel;
						}
					}
					case "mp3":
					{
						return WarcraftFileType.MP3Audio;
					}
					case "wav":
					{
						return WarcraftFileType.WaveAudio;
					}
					case "xml":
					{
						return WarcraftFileType.XML;
					}
					case "jpg":
					case "jpeg":
					{
						return WarcraftFileType.JPGImage;
					}
					case "gif":
					{
						return WarcraftFileType.GIFImage;
					}
					case "png":
					{
						return WarcraftFileType.PNGImage;
					}
					case "ini":
					{
						return WarcraftFileType.INI;
					}
					case "pdf":
					{
						return WarcraftFileType.PDF;
					}
					case "htm":
					case "html":
					{
						return WarcraftFileType.HTML;
					}
					default:
					{
						return WarcraftFileType.Unknown;
					}
				}
			}
			else
			{
				return WarcraftFileType.Directory;
			}
		}

		#region IEquatable implementation

		/// <summary>
		/// Determines whether the specified <see cref="System.Object"/> is equal to the current <see cref="FileReference"/>.
		/// </summary>
		/// <param name="obj">The <see cref="System.Object"/> to compare with the current <see cref="FileReference"/>.</param>
		/// <returns><c>true</c> if the specified <see cref="FileReference"/> is equal to the current
		/// <see cref="FileReference"/>; otherwise, <c>false</c>.</returns>
		public override bool Equals(object obj)
		{
			FileReference other = obj as FileReference;
			return other != null && Equals(other);
		}

		/// <summary>
		/// Determines whether the specified <see cref="FileReference"/> is equal to the current <see cref="FileReference"/>.
		/// </summary>
		/// <param name="other">The <see cref="FileReference"/> to compare with the current <see cref="FileReference"/>.</param>
		/// <returns><c>true</c> if the specified <see cref="FileReference"/> is equal to the current
		/// <see cref="FileReference"/>; otherwise, <c>false</c>.</returns>
		public bool Equals(FileReference other)
		{
			if (other != null)
			{
				return
					this.PackageGroup == other.PackageGroup &&
					this.PackageName == other.PackageName &&
					this.FilePath == other.FilePath;
			}
			return false;
		}

		#endregion

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="FileReference"/>.
		/// </summary>
		/// <returns>A <see cref="System.String"/> that represents the current <see cref="FileReference"/>.</returns>
		public override string ToString()
		{
			return $"{this.PackageName}:{this.FilePath}";
		}

		/// <summary>
		/// Serves as a hash function for a <see cref="FileReference"/> object.
		/// </summary>
		/// <returns>A hash code for this instance that is suitable for use in hashing algorithms and data structures such as a hash table.</returns>
		public override int GetHashCode()
		{
			return (
				this.PackageName.GetHashCode() +
				this.FilePath.GetHashCode() +
				this.PackageGroup.GroupName.GetHashCode()
			).GetHashCode();

		}
	}
}