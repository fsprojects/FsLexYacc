module JsonParsing


type JsonValue = 
  | Assoc of (string * JsonValue) list
  | Bool of bool
  | Float of float
  | Int of int
  | List of JsonValue list
  | Null
  | String of string


  //below function is not important, it simply prints values 
  static member print x = 
            match x with
            | Bool b -> sprintf "Bool(%b)" b
            | Float f -> sprintf "Float(%f)" f
            | Int d -> sprintf "Int(%d)" d
            | String s -> sprintf "String(%s)" s
            | Null ->  "Null()"
            | Assoc props ->  props 
                               |> List.map (fun (name,value) -> sprintf "\"%s\" : %s" name (JsonValue.print(value))) 
                               |> String.concat ","
                               |> sprintf "Assoc(%s)"
            | List values ->  values
                               |> List.map (fun value -> JsonValue.print(value)) 
                               |> String.concat ","
                               |> sprintf "List(%s)"
