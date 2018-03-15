# Using CNTK Managed API to predict iris species within the Iris dataset via REST

The classifier code is taken from excellent blog by Bahrudin Hrnjica (https://bhrnjica.net)
Windows hosting service added with WebApi support, for tests use ConsoleHost project.

Use of postman tool is highly recommended (https://www.getpostman.com) 
:)

# Use:

* First, to create and train model, send GET request (http://localhost:9000/api/v1/iris/train_model)
  Response will provide accuracy result
* To evaluate trained model send (http://localhost:9000/api/v1/iris/evaluate)
* Sample data:
  {"Token":"api_token", "Input":{"sepal_length":1.0,"sepal_width":2.0,"petal_length":1.0,"petal_width":3.0}}
* Token is not validated as this time.
  
*Dependencies*
* C#/.NET 4.7.1
* CNTK 2.4.0
* WInApi 5.2.4
* Owin 4.0
* Topshelf 4.0.3
* log4net 2.0.5 
