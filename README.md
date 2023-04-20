# HotAPI

Classe para criação de APIs encapsulando todas as dependências.
Permite a criação, com documentação, de forma extremamente simples:

	public class Program : HotAPIServer {
	    public static void Main(string[] args) {
	        MainDefault<Program>();
	    }
	}

	public class Calculadora : HotAPI {
	    /// <summary>
	    /// Subtrai dos números
	    /// </summary>
	    /// <param name="a">parâmetro 1</param>
	    /// <param name="b">parâmetro 2</param>
	    /// <returns>subtração retornada</returns>
	    public int Subtrai(int a, int b = 878) => a - b;

	    /// <summary>
	    /// Soma dois números
	    /// </summary>
	    /// <param name="a"></param>
	    /// <param name="b"></param>
	    /// <returns></returns>
	    public int Soma(int a, int b) => a + b;
	}

Para instalar o modelo de projeto da HotAPI para o Visual Studio 2022, use <a href="https://github.com/mrebello/HotAPI_Modelo/raw/master/HotAPI_Modelo_install.exe">este  instalador</a>.

(O instalador extrai o arquivo HotAPI_Modelo.zip para a pasta
%USERPROFILE%\Documents\Visual Studio 2022\Templates\ProjectTemplates)

Após a instalação, vá em criar novo projeto e o HotAPI_Modelo aparecerá como modelo de projeto a ser criado.

