grammar Mashd;

// Parser Rules
program         : (importStatement | definition | statement)* EOF 
                ;

importStatement : 'import' STRING                                         # ImportDeclaration
                ;

definition      : type identifier '(' parameters ')' block                # FunctionDeclaration
                ;

statement       : block
                | type identifier ';'                                     # VariableDeclaration
                | type identifier '=' expression ';'                      # VariableDeclarationWithAssignment
                | identifier '=' expression ';'                           # Assignment
                | identifier '+=' expression ';'                          # AddAssignment
                | identifier '-=' expression ';'                          # SubtractAssignment
                | identifier '*=' expression ';'                          # MultiplyAssignment
                | identifier '/=' expression ';'                          # DivideAssignment
                | identifier '??=' expression ';'                         # NullCoalescingAssignment
                | 'if' '(' expression ')' statement ('else' statement)?   # IfElseStatement
                | expression '?' statement ':' statement ';'              # TernaryStatement
                | 'return' expression ';'                                 # ReturnStatement
                ;

block           : '{' statements '}'                                      # BlockStatement

statements      : statement*                                              # SequentialStatements
                ;

// TODO flatten expressions

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
                : ID
                | 'true' | 'false'
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

// TODO make rules from parameters of the following tokens (Allan)

SCHEMA          : 'Schema' '{' .*? '}' ;  
DATASET         : 'Dataset' '{' .*? '}' ;  
MASHD           : 'Mashd' '{' .*? '}' ;   

ID              : [a-zA-Z_][a-zA-Z0-9_]* ;

// Terminal definitions
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
MULTILINECOMMENT
                : '/*' .*? '*/' -> skip ;
