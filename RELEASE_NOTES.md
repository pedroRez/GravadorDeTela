# Notas de Release

## Destaques
- Gravador de tela simples com foco em máquinas modestas (FPS padrão em 30 para evitar travamentos).
- Saída em vídeo configurável por controle de qualidade no app.
- `ffmpeg.exe` já incluído em `RecursosExternos`, dispensando instalações externas.
- Ícone personalizado já embutido no executável e instaladores baseados nele.

## Requisitos
- Windows com .NET Framework 4.8 instalado.
- Permissão para executar binários baixados da internet (pode ser necessário desbloquear o arquivo no Windows).

## Instalador e binários
- Baixe o instalador (ou o `.zip` com o executável) disponível nos **Assets** desta release.
- O executável de produção é gerado em `bin/x64/Release/GravadorDeTela.exe` quando a solução é compilada com `msbuild GravadorDeTela.sln /p:Configuration=Release`.

## Como usar
1. Abra o aplicativo.
2. Escolha a área da tela, o caminho de saída e ajuste a qualidade pelo controle do aplicativo.
3. Clique em **Iniciar** para começar a gravação e em **Parar** para finalizar.
4. O vídeo será salvo no local selecionado.

## Problemas conhecidos
- Em máquinas muito limitadas, aumentar a qualidade pode reduzir o desempenho; mantenha valores mais baixos se notar lentidão.
- A primeira execução pode ser bloqueada pelo SmartScreen do Windows; confirme que confia no aplicativo para prosseguir.

## Contato
Relate problemas ou sugerir melhorias abrindo uma issue no repositório.
