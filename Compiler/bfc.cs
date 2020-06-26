using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
/*
 
    https://cs.lmu.edu/~ray/notes/ohmexamples/
    
    Lexer(token)
    Parser(syntax, AST, LL parser) ? Validate? [] should be in pair
    Semantic Analyzer (type checking, Label checking and Flow control checking.)
    Code generator (emit IL)
    Optimizer (JIT...)

    Parser : https://www.cs.fsu.edu/~engelen/courses/COP402003/board.html#productio
    System.Reflection.Emit
    ILGenerator
*/
public class BFC {
  public string[] _args { get; }
  public BFC(string[] args) {
    this._args = args;
  }
  public void compile() {
    //if (this._args.Length < 1) throw new ArgumentNullException("no bf file");
    //string bf_filepath = this._args[0];
    //string str = File.ReadAllText(bf_filepath);
    string str = @"+[>[<->+[>+++>[+++++++++++>][]-[<]>-]]++++++++++<]>>>>>>----.<<+++.<-..+++.<-.>>>.<<.+++.------.>-.<<+.<.";
    //string str = @"[+++++++++++++-]";
    var tokens = new Lexer(str).lex();
    var ast = new Parser(tokens).ast();
    new CodeGenerator(ast).gen();
    //var visitor = new ASTVisitor(ast);
    //visitor.print();
  }
}

public enum Token_Enum {
  BeginLoop,/* [ */
  EndLoop, /* ]*/
  Input, /* , */
  Output,  /* .*/
  Increment, /* + */
  Decrement, /* - */
  MoveLeft,/*<*/
  MoveRight, /*>*/
}
public class Token {
  public char Literal { get; }
  public Token_Enum TokenType { get; }
  public int _pos;
  public bool IsLoopScan { get; set; }
  public Token(char literal, Token_Enum token_Enum, int pos) {
    TokenType = token_Enum;
    Literal = literal;
    _pos = pos;
  }
}
public class Lexer {
  private List<Token>tokens = new List<Token>();
  private string _str;
  public Lexer(string str) {
    this._str = str;
  }
  public List<Token> lex () {
    int len = this._str.Length;
    int i = 0;
    int j = 0;
    while(i < len) {
      switch(this._str[i]) {
        case '[': this.tokens.Add(new Token('[', Token_Enum.BeginLoop, j)); j++; break;
        case ']': this.tokens.Add(new Token(']', Token_Enum.EndLoop, j)); j++; break;
        case '+': this.tokens.Add(new Token('+', Token_Enum.Increment, j)); j++; break;
        case '-': this.tokens.Add(new Token('-', Token_Enum.Decrement, j)); j++; break;
        case ',': this.tokens.Add(new Token(',', Token_Enum.Input, j)); j++; break;
        case '.': this.tokens.Add(new Token('.', Token_Enum.Output, j)); j++; break;
        case '<': this.tokens.Add(new Token('<', Token_Enum.MoveLeft, j)); j++; break;
        case '>': this.tokens.Add(new Token('>', Token_Enum.MoveRight, j)); j++; break;
        default: break; // ignore Error token
      }
      i++;
    }
    return tokens;  
  }
}
/*
Program → Instr Program | ε
Instr → '+' | '-' | '>' | '<' | ',' | '.' | '[' Program ']'
LL(1)
*/
public class ASTNode {
  public ASTNode LeftNode { get; set; }
  public ASTNode MiddleNode { get; set; }
  public ASTNode RightNode { get; set; }
  public char Value { get; }
  public ASTNode() {
  }
  public ASTNode(Token token) {
    Value = token.Literal;
  }
}
public class Parser {
  private List<Token>_tokens = new List<Token>();
  private int _currentIndex;
  private int _endIndex;
  public Parser(List<Token> tokens) {
    _tokens = tokens;
    _currentIndex = 0;
    _endIndex = tokens.Count == 0 ? -1 : tokens.Count-1;
  }
  public ASTNode ast() {
    ASTNode ast = _program();
    return ast;
  }
  // [+]
  private ASTNode _program() {
    if (_endIndex == -1)  return null;
    if (_isNextTokenEOF()) return null;
    
    ASTNode leftNode = _instr();
    if (leftNode == null) return null;
    ASTNode rightNode = _program();
    if (rightNode == null) return leftNode;
    
    ASTNode ast = new ASTNode();
    ast.LeftNode = leftNode;
    ast.RightNode = rightNode;
    
    return ast;
  }
  private ASTNode _instr() {
    Token token = _getCurrentToken();
    if (token == null) return null;
    if (token.IsLoopScan == true) return null;

    if (isTerminals(token.Literal)){
      return new ASTNode(token);
    }
    else if (token.TokenType == Token_Enum.BeginLoop) {
      int searchIndex_right = _currentIndex;
      int bracket_right = 1;
      while(bracket_right > 0) {
        if (_tokens[searchIndex_right].TokenType == Token_Enum.EndLoop) bracket_right--;
        else if (_tokens[searchIndex_right].TokenType == Token_Enum.BeginLoop) bracket_right++;
        if (_endIndex < searchIndex_right || bracket_right == 0) break;
        searchIndex_right++;
      }
      if (bracket_right > 0 ) {
        throw new Exception("no found ]");
      }
      ASTNode ast = new ASTNode();
      ast.LeftNode = new ASTNode(token);
      ast.RightNode = new ASTNode(_tokens[searchIndex_right]);
      token.IsLoopScan = true;
      _tokens[searchIndex_right].IsLoopScan = true;
      ASTNode middleNode = _program();
      if (middleNode != null)  ast.MiddleNode = middleNode;
      
      return ast;
    }
    //extra ]
    throw new Exception("unkown error");
  }
  private bool isTerminals(char literal) {
    return literal == '+' || literal == '-' || literal == '>' || literal == '<' || literal == ',' || literal == '.';
  }
  private Token _getCurrentToken() {
    if (_endIndex < _currentIndex) return null;
    return _tokens[_currentIndex++];
  }
  private bool _isNextTokenEOF () {
    return _endIndex < _currentIndex;
  }
}
public class ASTVisitor {
  public ASTNode _ast { get; }
  public StringBuilder _ast_str {get; set ;}
  public ASTVisitor(ASTNode ast) {
    _ast = ast;
    _ast_str = new StringBuilder(); 
  }
  public void print() {
    walk(_ast, 0);
  }
  private void walk(ASTNode node, int layer) {
    if (node == null) return;
    if (node.Value != '\0') Console.WriteLine(new string(' ', layer) + node.Value);
    walk(node.LeftNode, layer+1);
    walk(node.MiddleNode, layer+1);
    walk(node.RightNode, layer+1);
  } 
}
//public class SemanticAnalyzer {}
public class CodeGenerator {
  public ASTNode _ast { get; }
  public CodeGenerator(ASTNode ast) {
    _ast = ast;
  }
  public void gen() {
    AppDomain ad = AppDomain.CurrentDomain;
    AssemblyName am = new AssemblyName();
    am.Name = "bfAsm";
    AssemblyBuilder ab = ad.DefineDynamicAssembly(am, AssemblyBuilderAccess.Save);
    ModuleBuilder mb = ab.DefineDynamicModule("bfMod", "bfAsm.exe");
    TypeBuilder tb = mb.DefineType("bfType", TypeAttributes.Public);
    MethodBuilder metb = tb.DefineMethod("codeGen", MethodAttributes.Public |
    MethodAttributes.Static, null, null);
    ab.SetEntryPoint(metb);

    ILGenerator il = metb.GetILGenerator();
    il.Emit(OpCodes.Ret);
    tb.CreateType();
    ab.Save("bfAsm.exe");
  }
  public void print() {
    walk(_ast, 0);
  }

  private void walk(ASTNode node, int layer) {
    if (node == null) return;
    if (node.Value != '\0') Console.WriteLine(new string(' ', layer) + node.Value);
    walk(node.LeftNode, layer+1);
    walk(node.MiddleNode, layer+1);
    walk(node.RightNode, layer+1);
  } 
}
