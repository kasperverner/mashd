# smashd

Smashd is a DSL for complex join and unions of datasources.

## Smashd sample

```
// importing artifacts from another .sm file
import "another_file.sm";

// defining schemas for data sets as a dictionary of field names, their types and corresponding column names
schema patient = {
  id: {
    type: int,
    name: "patient_id"
  },
  ...
};

schema operation = {
  id: {
    type: int,
    name: "operation_id"
  },
  ...
};

// defining data sets as a dictionary of adapter type and schema and optional take and skip parameters for debugging
// we will have 3 options for datasets - csv, sql query or sql table.

// a csv dataset can be defined with a file name, delimiter
dataSet patients = {
  adapter: "csv",
  schema: patient,
  file: "patients.csv",
  delimiter: ",",
  take: 1,
  skip: 100,
};

// a database dataset can be defined with a connection string and either a query
dataSet operations = {
  adapter: "postgresql",
  schema: operation,
  connectionString: "connection_string",
  query: "SELECT * FROM operations",
};

// custom match function for matching data sets
// the function should take 2 objects of the schemas being matched and return a boolean
bool customMatchFunction(patient p, operation o) {
  bool match = p.patientName == o.patientName;

  return match;
}

// utility function to convert a string to a dateTime object
dateTime toDate(string date) {
  return dateTime.Parse(date);
}

// transform requires a function that takes 2 parameters of the schemas being joined and returns a new schema
// the transform method can be used to transform the data sets before matching or on the strategy to define the output schema
schemaY transformMethod(patient p, operation o) {
  return {
    id: p.id * 2,
    name: p.name,
    operationId: o.id,
    operationName: o.name,
    operationDate: toDate(o.date)
  };
}

// defining a smashd for joining data sets
// smashd can be used to join or concatenate 2 data sets
// operations can be chained using dot-notation.

smashd smash = patients
  .smash(operations)
  // the match methods can be chained together and executed in order
  .match(patient.patientId, operation.patient_id)
  .fuzzyMatch(patient.patientId, operation.patient_id, 0.8)
  .functionMatch(customMatchFunction)
  // transform defines the output scheme of the smashd
  .transform(transformMethod);

// smash.join() for joining data sets horizontally
// smash.union() for concatenating data sets vertically (requires the same schema)
dataSet s3 = smash.join()

// after .join() or .union() another smash can be performed for further 

// a dataSet can be exported to a csv file using the toFile method
s3.toFile("output.csv");

// with a future implementation the output could be written to a database table using the toTable method
s3.toTable("connection_string", "table_name | query");
```

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
git clone https://github.com/kasperverner/smashd.git
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
