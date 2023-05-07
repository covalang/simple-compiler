grammar Grammar;

program: body EOF;

body: (statement ';' | functionDefinition)*;

statement
    : localDeclaration
    | assignment
    | return
    | functionInvocation
    ;

localDeclaration: 'local' assignment;
assignment: Id '=' expr;
return: 'return' expr?;
functionInvocation: Id '(' (expr (',' expr)*)? ')';
functionDefinition: 'func' Id '(' parameters? ')' ('{' body '}' | '=>' expr ';');
parameters: Id (',' Id)*;

expr
    : Id #variableReference
    | Number #numberLiteral
//    | String #stringLiteral
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

Id: [a-zA-Z_][a-zA-Z0-9_]*;
Number:  Digit+;
Digit: '0'..'9';
//String: '"' ( Escape | ~["\\] )* '"' ;
//Escape: '\\' [\\rnt0];
Comment: '//' ~('\r' | '\n')* '\r'? '\n' -> skip;
Whitespace: [ \t\r\n]+ -> skip;
