module FsLex.Core.Tests.UnicodeTests
open System
open System.Globalization
open FsLexYacc.FsLex
open Expecto

[<Tests>]
let tests =
    testList "Unicode" [
        test "Every unicode category should have a mapping" {
            let allUnicodeCategories = Enum.GetValues(typeof<UnicodeCategory>) |> Seq.cast<UnicodeCategory>
            let mappedUnicodeCategories = AST.unicodeCategories.Values

            Expect.containsAll mappedUnicodeCategories allUnicodeCategories "Not all unicode categories are mapped"
        }
        test "IsUnicodeCategory should recognize every encoded unicode category" {
            let unicodeCategoriesAsStrings = AST.unicodeCategories.Keys
            let encodedUnicodeCategories =
                unicodeCategoriesAsStrings
                |> Seq.map (fun uc -> AST.EncodeUnicodeCategory uc {unicode=true; caseInsensitive=false})

            Expect.all encodedUnicodeCategories AST.IsUnicodeCategory "Not all encoded unicode categories are recognized"
        }
    ]
