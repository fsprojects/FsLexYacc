(* (c) Microsoft Corporation 2005-2008.  *)

module internal FsLexYacc.FsLex.AST

open System.Collections.Generic
open System.Globalization
open Internal.Utilities.Text.Lexing

type Ident = string
type Code = string * Position

type Alphabet = uint32

let Eof : Alphabet = 0xFFFFFFFEu
let Epsilon : Alphabet = 0xFFFFFFFFu

let mutable unicode = false
let mutable caseInsensitive = false

let unicodeCategories =
 dict
  [| "Pe", UnicodeCategory.ClosePunctuation; // (Pe)
    "Pc", UnicodeCategory.ConnectorPunctuation; // (Pc)
    "Cc", UnicodeCategory.Control; // (Cc)
    "Sc", UnicodeCategory.CurrencySymbol; // (Sc)
    "Pd", UnicodeCategory.DashPunctuation; // (Pd)
    "Nd", UnicodeCategory.DecimalDigitNumber; // (Nd)
    "Me", UnicodeCategory.EnclosingMark; // (Me)
    "Pf", UnicodeCategory.FinalQuotePunctuation; // (Pf)
    "Cf", UnicodeCategory.Format; // (Cf)
    "Pi", UnicodeCategory.InitialQuotePunctuation; // (Pi)
    "Nl", UnicodeCategory.LetterNumber; // (Nl)
    "Zl", UnicodeCategory.LineSeparator; // (Zl)
    "Ll", UnicodeCategory.LowercaseLetter; // (Ll)
    "Sm", UnicodeCategory.MathSymbol; // (Sm)
    "Lm", UnicodeCategory.ModifierLetter; // (Lm)
    "Sk", UnicodeCategory.ModifierSymbol; // (Sk)
    "Mn", UnicodeCategory.NonSpacingMark; // (Mn)
    "Ps", UnicodeCategory.OpenPunctuation; // (Ps)
    "Lo", UnicodeCategory.OtherLetter; // (Lo)
    "Cn", UnicodeCategory.OtherNotAssigned; // (Cn)
    "No", UnicodeCategory.OtherNumber; // (No)
    "Po", UnicodeCategory.OtherPunctuation; // (Po)
    "So", UnicodeCategory.OtherSymbol; // (So)
    "Zp", UnicodeCategory.ParagraphSeparator; // (Zp)
    "Co", UnicodeCategory.PrivateUse; // (Co)
    "Zs", UnicodeCategory.SpaceSeparator; // (Zs)
    "Mc", UnicodeCategory.SpacingCombiningMark; // (Mc)
    "Cs", UnicodeCategory.Surrogate; // (Cs)
    "Lt", UnicodeCategory.TitlecaseLetter; // (Lt)
    "Lu", UnicodeCategory.UppercaseLetter; // (Lu)
  |]

let NumUnicodeCategories = unicodeCategories.Count
assert (NumUnicodeCategories = 30) // see table interpreter
let encodedUnicodeCategoryBase = 0xFFFFFF00u
let EncodeUnicodeCategoryIndex(idx:int) = encodedUnicodeCategoryBase + uint32 idx
let EncodeUnicodeCategory s =
    if not unicode then
        failwith "unicode category classes may only be used if --unicode is specified"
    if unicodeCategories.ContainsKey(s) then
        EncodeUnicodeCategoryIndex (int32 unicodeCategories.[s])
    else
        failwithf "invalid Unicode category: '%s'" s

let IsUnicodeCategory(x:Alphabet) = (encodedUnicodeCategoryBase <= x) && (x < encodedUnicodeCategoryBase + uint32 NumUnicodeCategories)
let UnicodeCategoryIndex(x:Alphabet) = (x - encodedUnicodeCategoryBase)

let numLowUnicodeChars = 128
assert (numLowUnicodeChars = 128) // see table interpreter
let specificUnicodeChars = new Dictionary<_,_>()
let specificUnicodeCharsDecode = new Dictionary<_,_>()

let EncodeChar(c:char) =
     let x = System.Convert.ToUInt32 c
     if unicode then
         if x < uint32 numLowUnicodeChars then x
         else
             if not(specificUnicodeChars.ContainsKey(c)) then
                 let idx = uint32 numLowUnicodeChars + uint32 specificUnicodeChars.Count
                 specificUnicodeChars.[c] <- idx
                 specificUnicodeCharsDecode.[idx] <- c
             specificUnicodeChars.[c]
     else
         if x >= 256u then failwithf "the Unicode character '%x' may not be used unless --unicode is specified" <| int c
         x

let DecodeChar(x:Alphabet) =
     if unicode then
         if x < uint32 numLowUnicodeChars then System.Convert.ToChar x
         else specificUnicodeCharsDecode.[x]
     else
         if x >= 256u then failwithf "the Unicode character '%x' may not be used unless --unicode is specified" x
         System.Convert.ToChar x



let NumSpecificUnicodeChars() = specificUnicodeChars.Count
let GetSpecificUnicodeChars() =
    specificUnicodeChars
        |> Seq.sortBy (fun (KeyValue(k,v)) -> v)
        |> Seq.map (fun (KeyValue(k,v)) -> k)

let GetSingleCharAlphabet() =
    if unicode
    then Set.ofList [ yield! { char 0 .. char <| numLowUnicodeChars-1 }
                      yield! GetSpecificUnicodeChars() ]
    else Set.ofList [ char 0 .. char 255 ]

let GetAlphabet() =
    if unicode
    then Set.ofList [ for c in GetSingleCharAlphabet() do yield EncodeChar c
                      for uc in 0 .. NumUnicodeCategories-1 do yield EncodeUnicodeCategoryIndex uc ]
    else GetSingleCharAlphabet() |> Seq.map EncodeChar |> set


//let DecodeAlphabet (x:Alphabet) = System.Convert.ToChar(x)

(*
for i in 0 .. 65535 do
    let c = char i
    if System.Char.GetUnicodeCategory c = System.Globalization.UnicodeCategory.PrivateUse then
        printfn "i = %x" i
*)

type Input =
  | Alphabet of Alphabet
  | UnicodeCategory of string
  | Any
  | NotCharSet of Set<Alphabet>
type Regexp =
  | Alt of Regexp list
  | Seq of Regexp list
  | Inp of Input
  | Star of Regexp
  | Macro of Ident
type Clause = Regexp * Code
type Spec =
    { TopCode: Code
      Macros: (Ident * Regexp) list
      Rules: (Ident * Ident list * Clause list) list
      BottomCode: Code }

type NodeId = int

type NfaNode =
    { Id: NodeId
      Name: string
      Transitions: Dictionary<Alphabet, NfaNode list>
      Accepted: (int * int) list }

type DfaNode =
    { Id: int
      Name: string
      mutable Transitions: (Alphabet * DfaNode) list
      Accepted: (int * int) list }

type MultiMap<'a,'b> = Dictionary<'a,'b list>
let LookupMultiMap (trDict:MultiMap<_,_>) a  =
    if trDict.ContainsKey(a) then trDict.[a] else []

let AddToMultiMap (trDict:MultiMap<_,_>) a b =
    let prev = LookupMultiMap trDict a
    trDict.[a] <- b::prev

type NfaNodeMap() =
    let map = new Dictionary<int,NfaNode>(100)
    member x.Item with get(nid) = map.[nid]
    member x.Count = map.Count

    member x.NewNfaNode(trs,ac) =
        let nodeId = map.Count+1 // ID zero is reserved
        let trDict = new Dictionary<_,_>(List.length trs)
        for (a,b) in trs do
           AddToMultiMap trDict a b

        let node : NfaNode = {Id=nodeId; Name=string nodeId; Transitions=trDict; Accepted=ac}
        map.[nodeId] <-node
        node

let LexerStateToNfa (macros: Map<string,_>) (clauses: Clause list) =

    /// Table allocating node ids
    let nfaNodeMap = new NfaNodeMap()

    /// Compile a regular expression into the NFA
    let rec CompileRegexp re dest =
        match re with
        | Alt res ->
            let trs = res |> List.map (fun re -> (Epsilon,CompileRegexp re dest))
            nfaNodeMap.NewNfaNode(trs,[])
        | Seq res ->
            List.foldBack (CompileRegexp) res dest
        | Inp (Alphabet c) ->
            if caseInsensitive && c <> Eof then
                let x = DecodeChar c
                let lowerCase = System.Char.ToLowerInvariant x
                let upperCase = System.Char.ToUpperInvariant x
                if lowerCase <> upperCase then
                    let encodedLowerCase = EncodeChar lowerCase
                    let encodedUpperCase = EncodeChar upperCase
                    nfaNodeMap.NewNfaNode([(encodedLowerCase, dest); (encodedUpperCase, dest)],[])
                else
                    nfaNodeMap.NewNfaNode([(c, dest)],[]) 
            else nfaNodeMap.NewNfaNode([(c, dest)],[])

        | Star re ->
            let nfaNode = nfaNodeMap.NewNfaNode([(Epsilon, dest)],[])
            let sre = CompileRegexp re nfaNode
            AddToMultiMap nfaNode.Transitions Epsilon sre
            nfaNodeMap.NewNfaNode([(Epsilon,sre); (Epsilon,dest)],[])
        | Macro m ->
            if not <| macros.ContainsKey(m) then failwithf "The macro %s is not defined" m
            CompileRegexp macros.[m] dest

        // These cases unwind the difficult cases in the syntax that rely on knowing the
        // entire alphabet.
        //
        // Note we've delayed the expension of these until we've worked out all the 'special' Unicode characters
        // mentioned in the entire lexer spec, i.e. we wait until GetAlphabet returns a reliable and stable answer.
        | Inp (UnicodeCategory uc) ->
            let re = Alt([ yield Inp(Alphabet(EncodeUnicodeCategory uc))
                           // Also include any specific characters in this category
                           for c in GetSingleCharAlphabet() do
                               if System.Char.GetUnicodeCategory(c) = unicodeCategories.[uc] then
                                    yield Inp(Alphabet(EncodeChar(c))) ])
            CompileRegexp re dest

        | Inp Any ->
            let re = Alt([ for n in GetAlphabet() do yield Inp(Alphabet(n)) ])
            CompileRegexp re dest

        | Inp (NotCharSet chars) ->
            let re = Alt [ // Include any characters from those in the alphabet besides those that are not immediately excluded
                           for c in GetSingleCharAlphabet() do
                               let ec = EncodeChar c
                               if not (chars.Contains(ec)) then
                                   yield Inp(Alphabet(ec))

                           // Include all unicode categories
                           // That is, negations _only_ exclude precisely the given set of characters. You can't
                           // exclude whole classes of characters as yet
                           if unicode then
                               let ucs = chars |> Set.map(DecodeChar >> System.Char.GetUnicodeCategory)
                               for KeyValue(nm,uc) in unicodeCategories do
                                   //if ucs.Contains(uc) then
                                   //    printfn "warning: the unicode category '\\%s' ('%O') is automatically excluded by this character set negation. Consider adding this to the negation." nm uc
                                   //else
                                       yield Inp(Alphabet(EncodeUnicodeCategory nm))
                         ]
            CompileRegexp re dest

    let actions = new System.Collections.Generic.List<_>()

    /// Compile an acceptance of a regular expression into the NFA
    let sTrans macros nodeId (regexp,code) =
        let actionId = actions.Count
        actions.Add(code)
        let sAccept = nfaNodeMap.NewNfaNode([],[(nodeId,actionId)])
        CompileRegexp regexp sAccept

    let trs = clauses |> List.mapi (fun n x -> (Epsilon,sTrans macros n x))
    let nfaStartNode = nfaNodeMap.NewNfaNode(trs,[])
    nfaStartNode,(actions |> Seq.readonly), nfaNodeMap

// TODO: consider a better representation here.
type internal NfaNodeIdSetBuilder = HashSet<NodeId>

type internal NfaNodeIdSet(nodes: NfaNodeIdSetBuilder) =
    // BEWARE: the next line is performance critical
    let s = nodes |> Seq.toArray
    do Array.sortInPlaceWith compare s // 19

    // These are all surprisingly slower (because they create two arrays):
    //let s = nodes |> Seq.toArray |> Array.sort
    //let s = nodes |> Seq.toArray |> Array.sortWith compare // 76
    //let s = nodes |> Seq.toArray |> (fun arr -> Array.sortInPlace arr; arr) // 76

    member x.Representation = s
    member x.Elements = s
    member x.Fold f z = Array.fold f z s
    interface System.IComparable with 
        member x.CompareTo(y:obj) =
            Array.compareWith compare x.Representation (y :?> NfaNodeIdSet).Representation

    override x.Equals(y:obj) = 
        match y with 
        | :? NfaNodeIdSet as y ->
            let xr = x.Representation
            let yr = y.Representation
            let n = yr.Length
            let rec go i = (i >= n) || (xr.[i] = yr.[i] && go (i+1))
            xr.Length = n && go 0
        | _ -> false

    override x.GetHashCode() = hash s

    member x.IsEmpty = (s.Length = 0)
    member x.Iterate f = Array.iter f s

type NodeSetSet = Set<NfaNodeIdSet>

let newDfaNodeId =
    let i = ref 0
    fun () -> let res = !i in incr i; res

let NfaToDfa (nfaNodeMap:NfaNodeMap) nfaStartNode =
    let rec EClosure1 (acc:NfaNodeIdSetBuilder) (n:NfaNode) =
        if not (acc.Contains(n.Id)) then
            acc.Add(n.Id) |> ignore
            if n.Transitions.ContainsKey(Epsilon) then
                match n.Transitions.[Epsilon] with
                | [] -> () // this Clause is an optimization - the list is normally empty
                | tr ->
                    //printfn "n.Id = %A, #Epsilon = %d" n.Id tr.Length
                    tr |> List.iter (EClosure1 acc)

    let EClosure (moves:list<NodeId>) =
        let acc = new NfaNodeIdSetBuilder(HashIdentity.Structural)
        for i in moves do
            EClosure1 acc nfaNodeMap.[i]
        new NfaNodeIdSet(acc)

    // Compute all the immediate one-step moves for a set of NFA states, as a dictionary
    // mapping inputs to destination lists
    let ComputeMoves (nset:NfaNodeIdSet) =
        let moves = new MultiMap<_,_>()
        nset.Iterate(fun nodeId ->
            for (KeyValue(inp,dests)) in nfaNodeMap.[nodeId].Transitions do
                if inp <> Epsilon then
                    match dests with
                    | [] -> ()  // this Clause is an optimization - the list is normally empty
                    | tr -> tr |> List.iter(fun dest -> AddToMultiMap moves inp dest.Id))
        moves

    let acc = new NfaNodeIdSetBuilder(HashIdentity.Structural)
    EClosure1 acc nfaStartNode
    let nfaSet0 = new NfaNodeIdSet(acc)

    let dfaNodes = Dictionary<NfaNodeIdSet,DfaNode>()

    let GetDfaNode nfaSet =
        if dfaNodes.ContainsKey(nfaSet) then
            dfaNodes.[nfaSet]
        else
            let dfaNode =
                { Id = newDfaNodeId()
                  Name = nfaSet.Fold (fun s nid -> nfaNodeMap.[nid].Name+"-"+s) ""
                  Transitions = []
                  Accepted= nfaSet.Elements
                            |> Seq.map (fun nid -> nfaNodeMap.[nid].Accepted)
                            |> List.concat }
            //printfn "id = %d" dfaNode.Id

            dfaNodes.Add(nfaSet,dfaNode)
            dfaNode

    let workList = Stack()
    workList.Push nfaSet0
    let doneSet = HashSet()

    //let count = ref 0
    while workList.Count <> 0 do
        let nfaSet = workList.Pop()
        if not <| doneSet.Contains(nfaSet) then
            let moves = ComputeMoves nfaSet
            for (KeyValue(inp,movesForInput)) in moves do
                assert (inp <> Epsilon)
                let moveSet = EClosure movesForInput
                if not moveSet.IsEmpty then
                    //incr count
                    let dfaNode = GetDfaNode nfaSet
                    dfaNode.Transitions <- (inp, GetDfaNode moveSet) :: dfaNode.Transitions
                    // printf "%d (%s) : %s --> %d (%s)\n" dfaNode.Id dfaNode.Name (match inp with EncodeChar c -> String.make 1 c | LEof -> "eof") moveSetDfaNode.Id moveSetDfaNode.Name
                    workList.Push(moveSet)

            doneSet.Add(nfaSet) |> ignore

    //printfn "count = %d" !count
    let ruleStartNode = GetDfaNode nfaSet0
    let ruleNodes =
        dfaNodes
        |> Seq.map (fun kvp -> kvp.Value)
        |> Seq.toList
        |> List.sortBy (fun s -> s.Id)
    ruleStartNode,ruleNodes

let Compile spec =
    let macros = Map.ofList spec.Macros
    List.foldBack
        (fun (name,args,clauses) (perRuleData,dfaNodes) ->
            let nfa, actions, nfaNodeMap = LexerStateToNfa macros clauses
            let ruleStartNode, ruleNodes = NfaToDfa nfaNodeMap nfa
            //printfn "name = %s, ruleStartNode = %O" name ruleStartNode.Id
            (ruleStartNode,actions) :: perRuleData, ruleNodes @ dfaNodes)
        spec.Rules
        ([],[])

