﻿/*********************************************
 * The following file was copied from: https://github.com/spacechase0/StardewValleyMods/blob/develop/JsonAssets/IApi.cs.
 *
 * The original license is as follows:
 *
 * MIT License
 *
 * Copyright (c) 2021 Chase W
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 *
 * *******************************************/

namespace AtraShared.Integrations.Interfaces;

/// <summary>
/// API to interface with Json Assets.
/// </summary>
/// <remarks>Copied from: https://github.com/spacechase0/StardewValleyMods/blob/develop/JsonAssets/IApi.cs .</remarks>
public interface IJsonAssetsAPI
{
    /// <summary>Load a folder as a Json Assets content pack.</summary>
    /// <param name="path">The absolute path to the content pack folder.</param>
    void LoadAssets(string path);

    /// <summary>Load a folder as a Json Assets content pack.</summary>
    /// <param name="path">The absolute path to the content pack folder.</param>
    /// <param name="translations">The translations to use for <c>TranslationKey</c> fields, or <c>null</c> to load the content pack's <c>i18n</c> folder if present.</param>
    void LoadAssets(string path, ITranslationHelper translations);

    /***********************
     * SECTION: GET ID BY TYPE.
     ***********************/

    /// <summary>
    /// Gets the object ID of an object declared through Json Assets.
    /// </summary>
    /// <param name="name">Name of object.</param>
    /// <returns>Integer object ID, or -1 if not found.</returns>
    int GetObjectId(string name);

    /// <summary>
    /// Gets the ID of an crop declared through Json Assets.
    /// </summary>
    /// <param name="name">Name of crop.</param>
    /// <returns>Integer crop ID, or -1 if not found.</returns>
    int GetCropId(string name);

    /// <summary>
    /// Gets the ID of a fruit tree declared through Json Assets.
    /// </summary>
    /// <param name="name">Name of fruit tree.</param>
    /// <returns>Integer fruit tree ID, or -1 if not found.</returns>
    int GetFruitTreeId(string name);

    /// <summary>
    /// Gets the ID of a bigCraftable declared through Json Assets.
    /// </summary>
    /// <param name="name">Name of the BigCraftable.</param>
    /// <returns>Integer BigCraftable ID, or -1 if not found.</returns>
    int GetBigCraftableId(string name);
    int GetHatId(string name);
    int GetWeaponId(string name);
    int GetClothingId(string name);
}