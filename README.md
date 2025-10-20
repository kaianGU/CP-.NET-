# Hashtag Generator API

Projeto simples de backend em **C# (ASP.NET Minimal API)** que gera hashtags automaticamente a partir de um texto usando um modelo de IA do Ollama (`llama3.2:3b`).

---

## Como Rodar / Testar

1. Certifique-se que o Ollama está rodando com o comando: [**`ollama serve`**], este comando pode ser executando no powershell.
2. Clone ou abra o projeto no VS Code.
3. Para testar a API, você pode usar a extensão [**REST Client**](https://marketplace.visualstudio.com/items?itemName=humao.rest-client):

   * Abra o arquivo `.http` incluído no projeto.
   * Clique em **Send Request**.
4. Alternativamente, você pode testar via **cURL** ou Postman/Thunder Client importando a collection fornecida.

---

## Exemplo de Requisição

```http
POST http://localhost:5140/hashtags
Content-Type: application/json

{
  "text": "jogos de futebol no brasil",
  "count": 8,
  "model": "llama3.2:3b"
}
```

Resposta esperada:

```json
{
  "model": "llama3.2:3b",
  "count": 8,
  "hashtags": ["#futebol", "#brasil", "#jogos", "#soccer", "#campeonato", "#torcida", "#gol", "#futeboldobrasil"]
}
```

---

## Links Úteis

* [Video explicativo do projeto no YouTube](https://www.youtube.com/watch?v=JctB88Nk0cg&t=2s)
* [Comando Ollama Serve](https://ollama.com/)
* [Extensão REST Client](https://marketplace.visualstudio.com/items?itemName=humao.rest-client)

---

## Observações

* Limite máximo de hashtags: 30.
* O campo `text` é obrigatório.
* Mensagens de erro detalhadas são retornadas em caso de problemas no JSON ou na comunicação com o modelo.
