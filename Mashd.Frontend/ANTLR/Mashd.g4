grammar Mashd;

program         : (importStatement | definition | statement)* EOF 
                ;

importStatement : 'import' TEXT ';'                                                             # ImportDeclaration
                ;

definition      : type ID '(' formalParameters ')' block                                        # FunctionDefinition
                ;               

formalParameters 
                : (type ID (',' type ID)*)?                                                     # ParameterList
                ;

statement       : if                                                                            # IfStatement
                | type ID ('=' expression)? ';'                                                 # VariableDeclaration                
                | ID '=' expression ';'                                                         # Assignment
                | expression ';'                                                                # ExpressionStatement
                | 'return' expression ';'                                                       # ReturnStatement                
                ;

if              : 'if' '(' expression ')' block ('else' (block | if))?                          # IfDefinition
                ;

block           : '{' statement* '}'                                                            # BlockDefinition
                ;

expression      : literal                                                                       # LiteralExpression
                | type                                                                          # TypeLitteralExpression                
                | ID                                                                            # IdentifierExpression
                | '(' expression ')'                                                            # ParenExpression
                | functionCall                                                                  # FunctionCallExpression
                | expression '.' ID                                                             # PropertyAccessExpression
                | expression '.' methodChain                                                    # MethodCallExpression
                | '-' expression                                                                # NegationExpression
                | '!' expression                                                                # NotExpression
                | <assoc=left> expression op=('*'|'/'|'%') expression                           # MultiplicativeExpression
                | <assoc=left> expression op=('+'|'-') expression                               # AdditiveExpression
                | <assoc=left> expression op=('<'|'<='|'>'|'>='|'=='|'!=') expression           # ComparisonExpression
                | <assoc=left> expression op=('&&'|'||') expression                             # LogicalExpression
                | expression '??' expression                                                    # NullishCoalescingExpression
                | expression '?' expression ':' expression                                      # TernaryExpression
                | expression '&' expression                                                     # DatasetCombineExpression
                | '{' (keyValuePair (',' keyValuePair)*)? '}'                                   # ObjectExpression    
                ;

literal
                : BOOLEAN                                                                       # BooleanLiteral
                | INTEGER                                                                       # IntegerLiteral
                | DATE                                                                          # DateLiteral
                | DECIMAL                                                                       # DecimalLiteral
                | TEXT                                                                          # TextLiteral                
                | NULL                                                                          # NullLiteral
                ;

keyValuePair    : ID ':' expression
                ;

actualParameters
                : expression (',' expression)*
                ;
              
functionCall    : ID '(' actualParameters? ')'
                ;

methodChain     : functionCall ('.' methodChain)?
                ;       

type            : BOOLEAN_TYPE | INTEGER_TYPE | DATE_TYPE | DECIMAL_TYPE | TEXT_TYPE 
                | SCHEMA_TYPE | DATASET_TYPE | MASHD_TYPE
                ;

// Lexer Rules
BOOLEAN_TYPE    : 'Boolean' ;
INTEGER_TYPE    : 'Integer' ;
DATE_TYPE       : 'Date' ;
DECIMAL_TYPE    : 'Decimal' ;
TEXT_TYPE       : 'Text' ;

SCHEMA_TYPE     : 'Schema' ;
DATASET_TYPE    : 'Dataset' ;
MASHD_TYPE      : 'Mashd' ;

INTEGER         : [0-9]+ ;
DECIMAL         : [0-9]+ '.' [0-9]+ ;
BOOLEAN         : 'true' | 'false' ;

NULL            : 'null' ;

TEXT            : '"' (~["\r\n\\] | '\\' .)* '"' ;

DATE            : '\'' ISO8601Date '\'' 
                | '"' ISO8601Date '"'
                ;

ID              : [a-zA-Z_][a-zA-Z0-9_]* ;

// Whitespace and comments
WS              : [ \t\r\n]+ -> skip
                ;

COMMENT         : '//' ~[\r\n]* -> skip
                ;

MULTILINE_COMMENT
                : '/*' .*? '*/' -> skip
                ;

// Fragmented rules
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