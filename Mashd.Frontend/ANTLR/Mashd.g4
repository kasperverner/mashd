grammar Mashd;

program         : (importStatement | definition | statement)* EOF 
                ;

importStatement : 'import' TEXT ';'                                         # ImportDeclaration
                ;

definition      : type ID '(' formalParameters ')' block                    # FunctionDefinition
                | schema ID '=' schemaObject ';'                            # SchemaDefinition
                | dataset ID '=' datasetObject ';'                          # DatasetDefinition
                | mashd ID '=' datasetObject '&' datasetObject ';'          # MashdDefinition
                ;

formalParameters 
                : (type ID (',' type ID)*)?                                 # ParameterList
                ;

statement       : if                                                        # IfStatement
                | type ID ('=' expression)? ';'                             # VariableDeclaration                
                | ID '=' expression ';'                                     # Assignment
                | ID '+=' expression ';'                                    # AddAssignment
                | ID '-=' expression ';'                                    # SubtractAssignment
                | ID '*=' expression ';'                                    # MultiplyAssignment
                | ID '/=' expression ';'                                    # DivisionAssignment
                | ID '??=' expression ';'                                   # NullCoalescingAssignment 
                | 'return' expression ';'                                   # ReturnStatement
                ;

if              : 'if' '(' expression ')' block ('else' (block | if))?     # IfDefinition
                ;

block           : '{' statement* '}'                                        # BlockDefinition
                ;

expression      : literal                                                   # LiteralExpression                                 
                | ID                                                        # IdentifierExpression
                | '(' expression ')'                                        # ParenExpression
                | functionCall                                              # FunctionCallExpression
                | expression '.' ID                                         # PropertyAccessExpression
                | expression '.' methodChain                                # MethodChainExpression
                | expression '++'                                           # PostIncrementExpression
                | expression '--'                                           # PostDecrementExpression
                | '++' expression                                           # PreIncrementExpression
                | '--' expression                                           # PreDecrementExpression
                | '-' expression                                            # NegationExpression
                | '!' expression                                            # NotExpression
                | expression '*' expression                                 # MultiplicationExpression
                | expression '/' expression                                 # DivisionExpression
                | expression '%' expression                                 # ModuloExpression
                | expression '+' expression                                 # AdditionExpression
                | expression '-' expression                                 # SubtractionExpression
                | expression '<' expression                                 # LessThanExpression
                | expression '<=' expression                                # LessThanEqualExpression
                | expression '>' expression                                 # GreaterThanExpression
                | expression '>=' expression                                # GreaterThanEqualExpression
                | expression '==' expression                                # EqualityExpression
                | expression '!=' expression                                # InequalityExpression
                | expression '??' expression                                # NullishCoalescingExpression
                | expression '&&' expression                                # LogicalAndExpression
                | expression '||' expression                                # LogicalOrExpression
                | expression '?' expression ':' expression                  # TernaryExpression
                | '{' (keyValuePair (',' keyValuePair)*)? '}'               # ObjectExpression    
                ;

literal
                : BOOLEAN                                                   # BooleanLiteral
                | INTEGER                                                   # IntegerLiteral
                | DATE                                                      # DateLiteral
                | DECIMAL                                                   # DecimalLiteral
                | TEXT                                                      # TextLiteral
                | NULL                                                      # NullLiteral
                ;

keyValuePair    : ID ':' expression
                ;

actualParameters
                : expression (',' expression)*
                ;

methodChain     : functionCall ('.' methodChain)?
                ;
                
functionCall    : ID '(' actualParameters? ')'
                ;

//Schema                
schemaObject
                : '{' schemaProperties? '}'               
                ;       

schemaProperties
                : schemaProperty (',' schemaProperty)*            
                ;

schemaProperty
                : ID ':' '{' schemaFieldProperty (',' schemaFieldProperty)* '}'  
                ;    
schemaFieldProperty
                : 'type' ':' type
                | 'name' ':' TEXT
                ;
                
//Dataset
datasetObject
                : '{' datasetProperties? '}'   # DatasetObjectExpression
                ;

datasetProperties
                : datasetProperty (',' datasetProperty)*  # DatasetPropertyList
                ;

datasetProperty
                : 'adapter' ':' TEXT                      # DatasetAdapter
                | 'source' ':' TEXT                       # DatasetSource
                | 'schema' ':' ID                         # DatasetSchema
                | 'delimiter' ':' TEXT                    # CsvDelimiter
                | 'query' ':' TEXT                        # DatabaseQuery
                | 'skip' ':' INTEGER                      # DatasetSkip
                ;


type            : 'Boolean' | 'Integer' | 'Date' | 'Decimal' | 'Text' | schema | dataset | mashd
                ;

schema          : 'Schema'                
                ;
                
dataset         : 'Dataset' 
                ;
                
mashd           : 'Mashd' 
                ;

// Lexer Rules
INTEGER         : [0-9]+ ;
DECIMAL         : [0-9]+ '.' [0-9]+ ;
TEXT            : '"' (~["\r\n\\] | '\\' .)* '"' ;
ID              : [a-zA-Z_][a-zA-Z0-9_]* ;

BOOLEAN         : 'true' | 'false' ;
NULL            : 'null' ;

DATE            : '\'' ISO8601Date '\'' 
                | '"' ISO8601Date '"'
                ;

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