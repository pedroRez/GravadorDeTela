# GravadorDeTela
Ferramenta de gravação de tela com foco em computadores fracos. O FPS padrão é 30 para reduzir travamentos em máquinas modestas.

## Requisitos
- Windows com .NET Framework 4.8 instalado
- Visual Studio 2019 ou mais recente, ou [MSBuild](https://learn.microsoft.com/visualstudio/msbuild/msbuild)
- `ffmpeg` (já incluído em `RecursosExternos/ffmpeg.exe`)

## Instalação
1. Clone este repositório.
2. Abra `GravadorDeTela.sln` no Visual Studio e restaure os pacotes NuGet.
3. Compile a solução (**Compilar > Compilar Solução**) e execute (**Depurar > Iniciar**).
4. Alternativamente, compile pela linha de comando com:
   ```bash
   msbuild GravadorDeTela.sln /p:Configuration=Release
   ```
   O executável será gerado em `bin/x64/Release/GravadorDeTela.exe`.

## Como usar
1. Execute `GravadorDeTela.exe`.
2. Escolha a área da tela e o caminho de saída.
3. Clique em **Iniciar** para começar a gravação e em **Parar** para finalizar.
4. O vídeo será salvo no local selecionado.

## Contribuindo
Contribuições são bem-vindas! Abra uma issue para relatar problemas ou envie um pull request com melhorias.

## Licença
Este projeto está licenciado sob a [Licença MIT](LICENSE).
