using System;
using Newtonsoft.Json;
using AGRO_GRAMM;
using System.Collections.Generic;




using System;



public class Parser {
	public const int _EOF = 0;
	public const int _id = 1;
	public const int _cte_I = 2;
	public const int _cte_F = 3;
	public const int _ctr_Str = 4;
	public const int _cbl = 5;
	public const int _cbr = 6;
	public const int _bl = 7;
	public const int _br = 8;
	public const int _pl = 9;
	public const int _pr = 10;
	public const int _comma = 11;
	public const int _semicolon = 12;
	public const int _add = 13;
	public const int _sub = 14;
	public const int _mul = 15;
	public const int _div = 16;
	public const int _equal = 17;
	public const int _dot = 18;
	public const int _sadd = 19;
	public const int _ssub = 20;
	public const int _sdiv = 21;
	public const int _smul = 22;
	public const int _increment = 23;
	public const int _decrement = 24;
	public const int _colon = 25;
	public const int _less = 26;
	public const int _greater = 27;
	public const int _lesseq = 28;
	public const int _greatereq = 29;
	public const int _equaleq = 30;
	public const int _different = 31;
	public const int _and = 32;
	public const int _or = 33;
	public const int maxT = 48;

	const bool _T = true;
	const bool _x = false;
	const int minErrDist = 2;
	
	public Scanner scanner;
	public Errors  errors;

	public Token t;    // last recognized token
	public Token la;   // lookahead token
	int errDist = minErrDist;

const int // types
	  invalid = Int32.MaxValue, undef = 0, t_int = 1, t_float = 2, t_char = 3, t_void = 4 ,t_obj = 5, t_string = 6;

const int // object kinds
	  var = 0, func = 1, temporal = 2;


int[] TERM_OPERATORS = { _mul, _div };
int[] EXP_OPERATORS = { _add, _sub };
int[] RELEXP_OPERATORS = { _and, _or };
int[] RELOP_OPERATORS = { _greater, _less, _greatereq, _lesseq, _equaleq, _different };

Dictionary<int, string> operandInts = JsonConvert.DeserializeObject<Dictionary<int, string>>(@$"{{
				{_add}:'+',
				{_sub}:'-',
				{_div}:'/',
				{_mul}:'*',
				{_sadd}:'+=',
				{_ssub}:'-=',
				{_sdiv}:'/=',
				{_smul}:'*=',
				{_increment}: '++',
				{_decrement}: '--',
				{_less}:'<',
				{_lesseq}:'<=',
				{_greater}:'>',
				{_greatereq}:'>=',
				{_equaleq}:'==',
				{_equal}:'=',
				{_different}:'!=',
				{_and}:'&&',
				{_and}:'and',
				{_or}:'or',
				{_or}:'||'
				}}");

Dictionary<int, string> typesInts = JsonConvert.DeserializeObject<Dictionary<int, string>>(@$"{{
				{invalid}: 'INVALID',
                {undef}:'UNDEFINED',
				{t_int}:'INT',
				{t_float}:'FLOAT',
				{t_char}:'CHAR',
				{t_void}:'VOID',
				{t_obj}:'OBJ',
				{t_string}:'STRING'
				}}");

SymbolTable   sTable;

Stack<String> stackOperand = new Stack<String>();
Stack<int>   stackOperator = new Stack<int>();
Stack<int>      stackTypes = new Stack<int>();
Stack<int>      stackJumps = new Stack<int>();

int tempCont = 0;

public List<Actions> program = new List<Actions>();

void pushToOperandStack(string id, SymbolTable st){
    // In order to push to the stack, we need to know the type of the id
    int typeId = st.getType(id);
    // Push the id
    stackOperand.Push(id);
    // Push the type
    stackTypes.Push(typeId);

}

string createTempInt(int tempp, SymbolTable st){
    string tempName;
    tempName = "_t" + tempCont;
    tempCont+=1;
    st.putSymbol(tempName, t_int, temporal);
    return tempName;
}

string createTempFloat(float tempp, SymbolTable st){
    string tempName;
    tempName = "_t" + tempCont;
    tempCont+=1;
    st.putSymbol(tempName, t_float, temporal);
    return tempName;
}

string createTempString(string tempp, SymbolTable st){
    string tempName;
    tempName = "_t" + tempCont;
    tempCont+=1;
    st.putSymbol(tempName, t_string, temporal);
    return tempName;
}

void checkAssign(SymbolTable st) {
    string leftOper;
    string rightOper;
    int leftType;
    int rightType;
    int operat;

    if(stackOperator.Count > 0) {
        // Get the data to create the quad
        rightType = stackTypes.Pop();
        rightOper = stackOperand.Pop();

        if (stackOperand.Count > 0) {
            leftType = stackTypes.Pop();
            leftOper = stackOperand.Pop();
        }
        else {
            leftType = rightType;
            leftOper = rightOper;
        }
        
        operat = stackOperator.Pop();
        
        Cuadruple quad = new Cuadruple(operat, leftOper, rightOper, leftOper, st, operandInts);

        // Check if cube operator is valid for these operands
        if (quad.typeOut == invalid)
        {
            SemErr("Invalid assignment: " + typesInts[leftType] + " " + operandInts[operat] + " " + typesInts[rightType]);
        }
        program.Add(quad);
    }
}

void check(SymbolTable st, int[] arr){
    string leftOper;
    string rightOper;
    int leftType;
    int rightType;
    int operat;
    string tempName;
    if(stackOperator.Count > 0){
        if(Array.Exists(arr, s => s.Equals(stackOperator.Peek()) )){
            // Get the data to create the quad
            rightType = stackTypes.Pop();
            rightOper = stackOperand.Pop();
            leftType = stackTypes.Pop();
            leftOper = stackOperand.Pop();
            operat = stackOperator.Pop();
            // Create the temporal variable
            tempName = "_t" + tempCont;
            tempCont+=1;
            Cuadruple quad = new Cuadruple(operat, leftOper, rightOper, tempName, st, operandInts);

            // Check if cube operator is valid for these operands
            if (quad.typeOut == invalid)
            {
                SemErr("Invalid operation: " + typesInts[leftType] + " " + operandInts[operat] + " " + typesInts[rightType]);
            }
            st.putSymbol(tempName, quad.typeOut, temporal);
            program.Add(quad);
            pushToOperandStack(tempName, st);
        }
    }
}

/*--------------------------------------------------------------------------*/    

bool IsFunctionCall(){
    scanner.ResetPeek();
    Token x = la; 
    while (x.kind == _id ) 
        x = scanner.Peek();
    return x.kind == _pl;
}

bool IsMethodCall() { 
    scanner.ResetPeek();
    Token x = la; 
    while (x.kind == _id || x.kind == _dot) 
        x = scanner.Peek();
    return x.kind == _pl;
} 

bool IsTypeFunction() {
    scanner.ResetPeek();
    Token next = scanner.Peek();
    next = scanner.Peek();
    return next.kind == _pl;
}

bool IsDecVars(){
    scanner.ResetPeek();
    Token x = scanner.Peek();
    while (x.kind == _id || x.kind == _comma) 
        x = scanner.Peek();
    return x.kind == _semicolon;
}



	public Parser(Scanner scanner) {
		this.scanner = scanner;
		errors = new Errors();
	}

	void SynErr (int n) {
		if (errDist >= minErrDist) errors.SynErr(la.line, la.col, n);
		errDist = 0;
	}

	public void SemErr (string msg) {
		if (errDist >= minErrDist) errors.SemErr(t.line, t.col, msg);
		errDist = 0;
	}
	
	void Get () {
		for (;;) {
			t = la;
			la = scanner.Scan();
			if (la.kind <= maxT) { ++errDist; break; }

			la = t;
		}
	}
	
	void Expect (int n) {
		if (la.kind==n) Get(); else { SynErr(n); }
	}
	
	bool StartOf (int s) {
		return set[s, la.kind];
	}
	
	void ExpectWeak (int n, int follow) {
		if (la.kind == n) Get();
		else {
			SynErr(n);
			while (!StartOf(follow)) Get();
		}
	}


	bool WeakSeparator(int n, int syFol, int repFol) {
		int kind = la.kind;
		if (kind == n) {Get(); return true;}
		else if (StartOf(repFol)) {return false;}
		else {
			SynErr(n);
			while (!(set[syFol, kind] || set[repFol, kind] || set[0, kind])) {
				Get();
				kind = la.kind;
			}
			return StartOf(syFol);
		}
	}

	
	void PROGRAM() {
		sTable = new SymbolTable();
		
		while (StartOf(1)) {
			DECLARATION();
		}
		MAIN();
	}

	void DECLARATION() {
		if (IsTypeFunction() ) {
			DEC_FUNC();
		} else if (StartOf(2)) {
			DEC_VARS();
		} else SynErr(49);
	}

	void MAIN() {
		sTable = sTable.newChildSymbolTable(); 
		Expect(37);
		Expect(5);
		if (IsDecVars() ) {
			DEC_VARS();
		} else if (StartOf(3)) {
			STATUTE();
		} else SynErr(50);
		while (StartOf(4)) {
			if (IsDecVars() ) {
				DEC_VARS();
			} else {
				STATUTE();
			}
		}
		Expect(6);
		sTable = sTable.parentSymbolTable; 
	}

	void DEC_FUNC() {
		string name; int type; 
		TYPE_FUNC(out type );
		IDENT(out name );
		sTable.putSymbol(name, type, func);
		       sTable = sTable.newChildSymbolTable(); 
		Expect(9);
		if (la.kind == 34 || la.kind == 35 || la.kind == 36) {
			PARAMS_FUNC();
		}
		Expect(10);
		Expect(5);
		if (StartOf(4)) {
			if (IsDecVars() ) {
				DEC_VARS();
			} else {
				STATUTE();
			}
			while (StartOf(4)) {
				if (IsDecVars() ) {
					DEC_VARS();
				} else {
					STATUTE();
				}
			}
		}
		if (la.kind == 39) {
			RETURN();
		}
		Expect(6);
		sTable = sTable.parentSymbolTable; 
	}

	void DEC_VARS() {
		string name; int type; string className; 
		if (la.kind == 1) {
			IDENT(out className );
			IDENT(out name );
			sTable.putSymbol(name, t_obj, var); 
			while (la.kind == 11) {
				Get();
				IDENT(out name );
				sTable.putSymbol(name, t_obj, var); 
			}
			Expect(12);
		} else if (la.kind == 34 || la.kind == 35 || la.kind == 36) {
			SIMPLE_TYPE(out type );
			IDENT(out name );
			sTable.putSymbol(name, type, var); 
			if (la.kind == 7) {
				Get();
				Expect(2);
				Expect(8);
				if (la.kind == 7) {
					Get();
					Expect(2);
					Expect(8);
				}
			}
			while (la.kind == 11) {
				Get();
				IDENT(out name );
				sTable.putSymbol(name, type, var); 
				if (la.kind == 7) {
					Get();
					Expect(2);
					Expect(8);
					if (la.kind == 7) {
						Get();
						Expect(2);
						Expect(8);
					}
				}
			}
			Expect(12);
		} else SynErr(51);
	}

	void IDENT(out string name ) {
		Expect(1);
		name = t.val; 
	}

	void SIMPLE_TYPE(out int type ) {
		type = undef; 
		if (la.kind == 34) {
			Get();
			type = t_int; 
		} else if (la.kind == 35) {
			Get();
			type = t_float; 
		} else if (la.kind == 36) {
			Get();
			type = t_char; 
		} else SynErr(52);
	}

	void TYPE_FUNC(out int type ) {
		type = undef; 
		if (la.kind == 34) {
			Get();
			type = t_int; 
		} else if (la.kind == 35) {
			Get();
			type = t_float; 
		} else if (la.kind == 36) {
			Get();
			type = t_char; 
		} else if (la.kind == 38) {
			Get();
			type = t_void; 
		} else SynErr(53);
	}

	void PARAMS_FUNC() {
		string name; int type; 
		SIMPLE_TYPE(out type );
		IDENT(out name );
		sTable.putSymbol(name, type, var); 
		while (la.kind == 11) {
			Get();
			SIMPLE_TYPE(out type );
			IDENT(out name );
			sTable.putSymbol(name, type, var); 
		}
	}

	void STATUTE() {
		if (la.kind == 40) {
			INPUT();
		} else if (la.kind == 41) {
			PRINT();
		} else if (IsFunctionCall() ) {
			FUNC_CALL();
		} else if (la.kind == 42) {
			CONDITIONAL();
		} else if (la.kind == 44) {
			WHILE();
		} else if (la.kind == 45) {
			FOR();
		} else if (la.kind == 1) {
			ASSIGN();
		} else SynErr(54);
	}

	void RETURN() {
		Expect(39);
		HYPER_EXP();
		Expect(12);
	}

	void INPUT() {
		Expect(40);
		Expect(9);
		VARIABLE_ASSIGN();
		Expect(10);
		Expect(12);
	}

	void PRINT() {
		Expect(41);
		Expect(9);
		EXP();
		while (la.kind == 11) {
			Get();
			HYPER_EXP();
		}
		Expect(10);
		Expect(12);
	}

	void FUNC_CALL() {
		string name; 
		IDENT(out name );
		Expect(9);
		if (StartOf(5)) {
			EXP();
			while (la.kind == 11) {
				Get();
				EXP();
			}
		}
		Expect(10);
		Expect(12);
	}

	void CONDITIONAL() {
		Expect(42);
		Expect(9);
		HYPER_EXP();
		Expect(10);
		BLOCK();
		if (la.kind == 43) {
			Get();
			BLOCK();
		}
	}

	void WHILE() {
		Expect(44);
		Expect(9);
		HYPER_EXP();
		Expect(10);
		BLOCK();
	}

	void FOR() {
		Expect(45);
		Expect(9);
		ASSIGN();
		Expect(12);
		HYPER_EXP();
		Expect(12);
		ASSIGN();
		Expect(10);
		BLOCK();
	}

	void ASSIGN() {
		VARIABLE_ASSIGN();
		if (StartOf(6)) {
			if (StartOf(7)) {
				SHORT_ASSIGN();
			} else {
				Get();
				stackOperator.Push(_equal); 
			}
			HYPER_EXP();
		} else if (la.kind == 23 || la.kind == 24) {
			STEP();
		} else SynErr(55);
		Expect(12);
		checkAssign(sTable); 
	}

	void HYPER_EXP() {
		SUPER_EXP();
		while (StartOf(8)) {
			REL_EXP();
			SUPER_EXP();
		}
		check(sTable, RELEXP_OPERATORS); 
	}

	void EXP() {
		TERM();
		while (la.kind == 13 || la.kind == 14) {
			if (la.kind == 13) {
				Get();
				stackOperator.Push(_add); 
			} else {
				Get();
				stackOperator.Push(_sub);
			}
			TERM();
		}
		check(sTable, EXP_OPERATORS); 
	}

	void TERM() {
		FACT();
		while (la.kind == 15 || la.kind == 16) {
			if (la.kind == 15) {
				Get();
				stackOperator.Push(_mul); 
			} else {
				Get();
				stackOperator.Push(_div); 
			}
			FACT();
		}
		check(sTable, TERM_OPERATORS); 
	}

	void VARIABLE_ASSIGN() {
		string name; 
		IDENT(out name );
		pushToOperandStack(name, sTable); 
		if (la.kind == 7) {
			Get();
			EXP();
			Expect(8);
			if (la.kind == 7) {
				Get();
				EXP();
				Expect(8);
			}
		}
	}

	void SHORT_ASSIGN() {
		if (la.kind == 19) {
			Get();
			stackOperator.Push(_sadd); 
		} else if (la.kind == 20) {
			Get();
			stackOperator.Push(_ssub); 
		} else if (la.kind == 22) {
			Get();
			stackOperator.Push(_smul); 
		} else if (la.kind == 21) {
			Get();
			stackOperator.Push(_sdiv); 
		} else SynErr(56);
	}

	void STEP() {
		if (la.kind == 23) {
			Get();
			stackOperator.Push(_increment); 
		} else if (la.kind == 24) {
			Get();
			stackOperator.Push(_decrement); 
		} else SynErr(57);
	}

	void BLOCK() {
		Expect(5);
		while (StartOf(3)) {
			STATUTE();
		}
		Expect(6);
	}

	void FACT() {
		if (la.kind == 9) {
			Get();
			stackOperator.Push(_pl); 
			HYPER_EXP();
			Expect(10);
			stackOperator.Pop(); 
		} else if (StartOf(9)) {
			if (la.kind == 13 || la.kind == 14) {
				if (la.kind == 13) {
					Get();
				} else {
					Get();
				}
			}
			if (la.kind == 2) {
				Get();
				pushToOperandStack(createTempInt(Int32.Parse(t.val), sTable), sTable); 
			} else if (la.kind == 3) {
				Get();
				pushToOperandStack(createTempFloat(float.Parse(t.val), sTable), sTable); 
			} else SynErr(58);
		} else if (la.kind == 4) {
			Get();
			pushToOperandStack(createTempString(t.val, sTable), sTable); 
		} else if (la.kind == 1) {
			VARIABLE_FACT();
		} else SynErr(59);
	}

	void VARIABLE_FACT() {
		string name; 
		IDENT(out name );
		pushToOperandStack(name, sTable); 
		if (la.kind == 7 || la.kind == 9) {
			if (la.kind == 7) {
				Get();
				EXP();
				Expect(8);
				if (la.kind == 7) {
					Get();
					EXP();
					Expect(8);
				}
			} else {
				Get();
				if (StartOf(5)) {
					EXP();
					while (la.kind == 11) {
						Get();
						EXP();
					}
				}
				Expect(10);
			}
		}
	}

	void SUPER_EXP() {
		EXP();
		while (StartOf(10)) {
			REL_OP();
			EXP();
		}
		check(sTable, RELOP_OPERATORS); 
	}

	void REL_EXP() {
		if (la.kind == 46) {
			Get();
			stackOperator.Push(_and); 
		} else if (la.kind == 32) {
			Get();
			stackOperator.Push(_and); 
		} else if (la.kind == 47) {
			Get();
			stackOperator.Push(_or); 
		} else if (la.kind == 33) {
			Get();
			stackOperator.Push(_or); 
		} else SynErr(60);
	}

	void REL_OP() {
		if (la.kind == 26 || la.kind == 27) {
			if (la.kind == 27) {
				Get();
				stackOperator.Push(_greater); 
			} else {
				Get();
				stackOperator.Push(_less); 
			}
		} else if (la.kind == 28 || la.kind == 29) {
			if (la.kind == 29) {
				Get();
				stackOperator.Push(_greatereq); 
			} else {
				Get();
				stackOperator.Push(_lesseq); 
			}
		} else if (la.kind == 30 || la.kind == 31) {
			if (la.kind == 30) {
				Get();
				stackOperator.Push(_equaleq); 
			} else {
				Get();
				stackOperator.Push(_different); 
			}
		} else SynErr(61);
	}



	public void Parse() {
		la = new Token();
		la.val = "";		
		Get();
		PROGRAM();
		Expect(0);

	}
	
	static readonly bool[,] set = {
		{_T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x},
		{_x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_T, _T,_x,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x},
		{_x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_T, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x},
		{_x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _T,_T,_T,_x, _T,_T,_x,_x, _x,_x},
		{_x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_T, _T,_x,_x,_x, _T,_T,_T,_x, _T,_T,_x,_x, _x,_x},
		{_x,_T,_T,_T, _T,_x,_x,_x, _x,_T,_x,_x, _x,_T,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_x,_T, _T,_T,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _T,_T,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _T,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_T, _x,_x},
		{_x,_x,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_T, _T,_T,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x}

	};
} // end Parser


public class Errors {
	public int count = 0;                                    // number of errors detected
	public System.IO.TextWriter errorStream = Console.Out;   // error messages go to this stream
	public string errMsgFormat = "-- line {0} col {1}: {2}"; // 0=line, 1=column, 2=text

	public virtual void SynErr (int line, int col, int n) {
		string s;
		switch (n) {
			case 0: s = "EOF expected"; break;
			case 1: s = "id expected"; break;
			case 2: s = "cte_I expected"; break;
			case 3: s = "cte_F expected"; break;
			case 4: s = "ctr_Str expected"; break;
			case 5: s = "cbl expected"; break;
			case 6: s = "cbr expected"; break;
			case 7: s = "bl expected"; break;
			case 8: s = "br expected"; break;
			case 9: s = "pl expected"; break;
			case 10: s = "pr expected"; break;
			case 11: s = "comma expected"; break;
			case 12: s = "semicolon expected"; break;
			case 13: s = "add expected"; break;
			case 14: s = "sub expected"; break;
			case 15: s = "mul expected"; break;
			case 16: s = "div expected"; break;
			case 17: s = "equal expected"; break;
			case 18: s = "dot expected"; break;
			case 19: s = "sadd expected"; break;
			case 20: s = "ssub expected"; break;
			case 21: s = "sdiv expected"; break;
			case 22: s = "smul expected"; break;
			case 23: s = "increment expected"; break;
			case 24: s = "decrement expected"; break;
			case 25: s = "colon expected"; break;
			case 26: s = "less expected"; break;
			case 27: s = "greater expected"; break;
			case 28: s = "lesseq expected"; break;
			case 29: s = "greatereq expected"; break;
			case 30: s = "equaleq expected"; break;
			case 31: s = "different expected"; break;
			case 32: s = "and expected"; break;
			case 33: s = "or expected"; break;
			case 34: s = "\"int\" expected"; break;
			case 35: s = "\"float\" expected"; break;
			case 36: s = "\"char\" expected"; break;
			case 37: s = "\"main\" expected"; break;
			case 38: s = "\"void\" expected"; break;
			case 39: s = "\"return\" expected"; break;
			case 40: s = "\"input\" expected"; break;
			case 41: s = "\"print\" expected"; break;
			case 42: s = "\"if\" expected"; break;
			case 43: s = "\"else\" expected"; break;
			case 44: s = "\"while\" expected"; break;
			case 45: s = "\"for\" expected"; break;
			case 46: s = "\"and\" expected"; break;
			case 47: s = "\"or\" expected"; break;
			case 48: s = "??? expected"; break;
			case 49: s = "invalid DECLARATION"; break;
			case 50: s = "invalid MAIN"; break;
			case 51: s = "invalid DEC_VARS"; break;
			case 52: s = "invalid SIMPLE_TYPE"; break;
			case 53: s = "invalid TYPE_FUNC"; break;
			case 54: s = "invalid STATUTE"; break;
			case 55: s = "invalid ASSIGN"; break;
			case 56: s = "invalid SHORT_ASSIGN"; break;
			case 57: s = "invalid STEP"; break;
			case 58: s = "invalid FACT"; break;
			case 59: s = "invalid FACT"; break;
			case 60: s = "invalid REL_EXP"; break;
			case 61: s = "invalid REL_OP"; break;

			default: s = "error " + n; break;
		}
		errorStream.WriteLine(errMsgFormat, line, col, s);
		count++;
	}

	public virtual void SemErr (int line, int col, string s) {
		errorStream.WriteLine(errMsgFormat, line, col, s);
		count++;
	}
	
	public virtual void SemErr (string s) {
		errorStream.WriteLine(s);
		count++;
	}
	
	public virtual void Warning (int line, int col, string s) {
		errorStream.WriteLine(errMsgFormat, line, col, s);
	}
	
	public virtual void Warning(string s) {
		errorStream.WriteLine(s);
	}
} // Errors


public class FatalError: Exception {
	public FatalError(string m): base(m) {}
}
