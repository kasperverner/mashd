grammar Mashd;

// Parser Rules
program         : 'import' STRING program 
                | definition program 
                | statement program 
                | EOF 
                ;

definition      : type identifier '(' parameters ')' | type identifier '=' expression ;

statement       : statement statement                          // Sequence
                | '{' (type identifier ';')* statement '}'     // BlockStatement
                | identifier '=' expression                    // Assignment
                | identifier '+=' expression                   // Add assignment
                | identifier '-=' expression                   // Subtract assignment
                | identifier '*=' expression                   // Multiply assignment
                | identifier '/=' expression                   // Divide assignment
                | identifier '??=' expression                  // Null coalescing assignment
                | 'if' '(' expression ')' statement            // If statement
                | 'if' '(' expression ')' statement 'else' statement  // If-else statement
                | expression '?' statement ':' statement       // Ternary statement
                | 'return' expression                          // Return statement
                ;


expression      : logicalExpression
                ;

logicalExpression
                : equalityExpression ('||' equalityExpression)*
                | equalityExpression ('&&' equalityExpression)*
                | equalityExpression ('??' equalityExpression)*
                ;

equalityExpression
                : relationExpression ('==' relationExpression | '!=' relationExpression)*
                ;

relationExpression
                : binaryExpression ('<' binaryExpression | '<=' binaryExpression | '>' binaryExpression | '>=' binaryExpression)*
                ;

binaryExpression
                : multiplyExpression ('+' multiplyExpression | '-' multiplyExpression)*
                ;

multiplyExpression
                : unaryExpression ('*' unaryExpression | '/' unaryExpression | '%' unaryExpression)*
                ;

unaryExpression : ('-' | '!') unaryExpression
                | postfixExpression
                ;

postfixExpression
                : primaryExpression ('++' | '--')?
                ;

primaryExpression
                : identifier
                | Boolean
                | Integer
                | Date
                | Decimal
                | Text
                | Schema
                | Dataset
                | Mashd
                | 'null'
                | '{' (keyValuePair (',' keyValuePair)*)? '}' 
                | '(' expression ')'
                ;

keyValuePair    : identifier ':' expression
                ;
    
parameters      : (type identifier (',' type identifier)*)? ;

expressionList  : (expression (',' expression)*)? ;

type            : 'Boolean' | 'Integer' | 'Date' | 'Decimal' | 'Text' | 'Schema' | 'Dataset' | 'Mashd' ;


// Lexer Rules
ID              : [a-zA-Z_][a-zA-Z0-9_]* ;
INTEGER         : [0-9]+ ;
DECIMAL         : [0-9]+ '.' [0-9]+ ;
STRING          : '"' (~["\r\n\\] | '\\' .)* '"' ;
DATE            : '\'' ISO8601Date '\'' 
                | '"' ISO8601Date '"'
                ;

fragment ISO8601Date
                : YEAR '-' MONTH '-' DAY                                                    // YYYY-MM-DD
                | YEAR '-' MONTH '-' DAY 'T' HOUR ':' MINUTE ':' SECOND                     // YYYY-MM-DDThh:mm:ss
                | YEAR '-' MONTH '-' DAY 'T' HOUR ':' MINUTE ':' SECOND '.' MILLISECOND     // YYYY-MM-DDThh:mm:ss.sss
                | YEAR '-' MONTH '-' DAY 'T' HOUR ':' MINUTE ':' SECOND 'Z'                 // YYYY-MM-DDThh:mm:ssZ
                | YEAR '-' MONTH '-' DAY 'T' HOUR ':' MINUTE ':' SECOND '.' MILLISECOND 'Z' // YYYY-MM-DDThh:mm:ss.sssZ
                | YEAR '-' MONTH '-' DAY 'T' HOUR ':' MINUTE ':' SECOND TZ                  // YYYY-MM-DDThh:mm:ss±hh:mm
                | YEAR '-' MONTH '-' DAY 'T' HOUR ':' MINUTE ':' SECOND '.' MILLISECOND TZ  // YYYY-MM-DDThh:mm:ss.sss±hh:mm
                ;

fragment YEAR   : [0-9][0-9][0-9][0-9] ;
fragment MONTH  : [0-1][0-9] ;
fragment DAY    : [0-3][0-9] ;
fragment HOUR   : [0-2][0-9] ;
fragment MINUTE : [0-5][0-9] ;
fragment SECOND : [0-5][0-9] ;
fragment MILLISECOND 
                : [0-9]+ ;
fragment TZ     : ('+' | '-') HOUR ':' MINUTE ;

SCHEMA          : 'Schema' '{' .*? '}' ;  
DATASET         : 'Dataset' '{' .*? '}' ;  
MASHD           : 'Mashd' '{' .*? '}' ;   

// Terminal definitions
identifier      : ID ;
Boolean         : 'true' | 'false' ;
Integer         : INTEGER ;
Date            : DATE ;
Decimal         : DECIMAL ;
Text            : STRING ;
Schema          : SCHEMA ;
Dataset         : DATASET ;
Mashd           : MASHD ;

// Whitespace and comments
WS              : [ \t\r\n]+ -> skip ;
COMMENT         : '//' ~[\r\n]* -> skip ;
