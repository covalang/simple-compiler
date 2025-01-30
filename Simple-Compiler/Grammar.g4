grammar Grammar;

program: body EOF;

body: (statement ';' | functionDefinition)*;

statement
    : localDeclaration
    | assignment
    | return
    | functionInvocation
    ;

localDeclaration: 'local' name=Id (':' type=Id)? '=' expr;
assignment: Id '=' expr;
return: 'return' expr?;
functionInvocation: Id '(' (expr (',' expr)*)? ')';
functionDefinition: 'func' Id '(' params? ')' ('{' body '}' | '=>' expr ';');
params: param (',' param)*;
param: name=Id ':' type=Id;

expr
    : Id #variableReference
    | Integer #integerLiteral
    | Char #charLiteral
    | String #stringLiteral
    | functionInvocation #function
    | '(' expr ')' #subExpr
    | expr binOp expr #binOpExpr
    ;

binOp
    : '*' #mul
    | '/' #div
    | '+' #add
    | '-' #sub
    ;

Integer: Number IntegerSuffix?;
IntegerSuffix: IntegerSignedness IntegerStorageSize;
IntegerSignedness: 'u' | 'i';
IntegerStorageSize: '8' | '16' | '32' | '64' | '128';

HexadecimalPrefix: '0x';

Id: [a-zA-Z_][a-zA-Z0-9_]*;

Number:  Digit (Digit | '_')*;
Digit: '0'..'9';

Char: '\'' ( Escape | ~['\\] ) '\'';
String: '"' ( Escape | ~["\\] )* '"' ;
Escape: '\\' [\\rnt0];

Comment: '//' ~('\r' | '\n')* '\r'? '\n' -> skip;
Whitespace: [ \t\r\n]+ -> skip;

Sign: [+-];
HexadecimalDigit: [0-9a-fA-F];
DecimalDigit: [0-9];
BinaryDigit: [01];
