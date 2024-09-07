# CLAI (Command Line AI)

CLAI is a command-line tool that interacts with OpenAI's language model to provide code assistance. It takes user input, optionally with specified language and a context file, and returns the full response, or just the code blocks if a langauge is specified. It currently only works on Mac and Linux.

# Usage
## Configuration

Ensure you have your OpenAI API key set up. You can either:

1. Create a `.clai.env` file in your home directory with the following content:

    ```
    OPENAI_API_KEY="your_openai_api_key"
    ```

2. Or set the `OPENAI_API_KEY` environment variable in your shell:

    ```sh
    export OPENAI_API_KEY="your_openai_api_key"
    ```

## Usage

```sh
dotnet build && mv ./bin/Debug/net8.0/CLAI ./clai
./clai [-l|<LANGUAGE>] [-f|<CONTEXT_FILE>] <INPUT>
```

### Example

```sh
clai -l csharp -f Program.cs "Write an interface for my Entity class."
```

### Options

- `-l <LANGUAGE>`: Specify the preferred programming language.
- `-f <CONTEXT_FILE>`: File containing additional context.
- `<INPUT>`: The input query for which you need assistance. If not provided, you can use Vim to edit the input text.

# Development
## Prerequisites

- .NET SDK (version 5.0 or higher)
- OpenAI API Key

## Installation

1. Clone the repository:

    ```sh
    git clone https://github.com/yourusername/clai.git
    cd clai
    ```

2. Build the project:

    ```sh
    dotnet build
    ```

## License

This project is licensed under the MIT License. See the LICENSE file for details.