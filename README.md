# Mashd

Mashd is a DSL for complex join and unions of datasources.

## Project Structure

- Mashd.Application: The executable entry point
- Mashd.Test: The entry point for unit and integration tests
- Mashd.Frontend: DSL lexer and processor (Antlr generated)
- Mashd.Backend: DSL interpretation logic

## Git
Branch naming: `feature/<feature-name>`, `fix/<bug-name>`

Commit message: `feature: <feature-name> added`, `fix: <bug-name>`

Pull request title: `Feature: <feature-name>`, `Fix: <bug-name>`

Pull request description: Describe the changes made in the pull request

***IMPORTANT***
Always create a new branch for your changes and open a pull request for approval before merging your changes into the main branch.

Never push directly to the main branch.

## Start contributing

### Clone the repository

```bash
cd <path-to-your-projects-folder>
git clone https://github.com/kasperverner/mashd.git
```

### Create a new branch

The main branch is protected, so you need to create a new branch for your changes. The branch name should be `feature/<feature-name>` or `fix/<bug-name>`. You can create a new branch with the following command:

```bash
git checkout -b feature/<feature-name>
```

### Commit your changes

Commit your changes with the following command:

```bash
git add .
git commit -m "feature: <feature-name> added"
```

### Merge the main branch

Before you push your changes, you need to merge the main branch into your branch to avoid conflicts.

```bash
git switch main
git pull
git switch feature/<feature-name>
git merge main
```

### Push your changes

Push your changes to the remote repository with the following command:

```bash
git push origin feature/<feature-name>
```

### Create a pull request

Open a pull request using github.com, github cli, github desktop or another tool capable of initiating a pull request and describe the changes made in the branch you wish to merge to main.

Assign a reviewer to the pull request for approval to have your changes merged into the main branch.
