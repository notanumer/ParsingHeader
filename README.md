Дефолтное поведение при передаче массивов через заголовк, это парсинг по ','
Цель задания: интегрироваться в ModelBinder  так, что бы парсинг происходит и по ' '
Кейсы:
"Hello": "1 2 3" -> new string[]{ "1", "2", "3" }
"Hello": "1 2 3,   1,3" -> new string[]{ "1", "2", "3", "1", "3" }
В проекте уже создан контроллер, его трогать не надо, он для проверки работы приложения
