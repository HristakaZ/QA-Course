using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using DataAccess.Repositories;
using DataStructure;
using Hotel_API_Project.Data;
using Hotel_API_Project.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Hotel_API_Project.Controllers.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private IEmployeeRepository iEmployeeRepository;
        private IPositionRepository iPositionRepository;
        private IUnitOfWork iUnitOfWork;
        private HtmlEncoder htmlEncoder;
        public EmployeeController(IEmployeeRepository iEmployeeRepository, IPositionRepository iPositionRepository,
            IUnitOfWork iUnitOfWork, HtmlEncoder htmlEncoder)
        {
            this.iEmployeeRepository = iEmployeeRepository;
            this.iPositionRepository = iPositionRepository;
            this.iUnitOfWork = iUnitOfWork;
            this.htmlEncoder = htmlEncoder;
        }
        // GET: api/<EmployeeController>
        [HttpGet, Authorize]
        public IActionResult GetEmployees()
        {
            List<EmployeeApplicationUser> employees = iEmployeeRepository.GetEmployees();
            List<PositionApplicationRole> positions = iPositionRepository.GetPositions();
            /*encoding(against xss) at the get request, so as to store the entity column in its plain form in the database*/
            if (employees != null)
            {
                employees.ForEach(x =>
                {
                    if (x != null)
                    {
                        string encodedEmployeeUserName = htmlEncoder.Encode(x.UserName);
                        x.UserName = encodedEmployeeUserName;
                        if (x.Position != null)
                        {
                            string encodedEmployeePosition = htmlEncoder.Encode(x.Position.Name);
                            x.Position.Name = encodedEmployeePosition;
                        }
                        if (x.Position == null)
                        {
                            x.Position = positions.Where(x => x.Name == "No Position!").FirstOrDefault();
                        }
                    }
                });
                return Ok(employees);
            }
            return NotFound("No employees were found!");
        }

        // GET api/<EmployeeController>/5
        [HttpGet("{id}", Name = "GetEmployeeByID"), Authorize]
        public IActionResult GetEmployeeByID(int id)
        {
            EmployeeApplicationUser employee = iEmployeeRepository.GetEmployeeByID(id);
            if (employee != null)
            {
                return Ok(employee);
            }
            else
            {
                return NotFound("Employee with ID " + id.ToString() + " was not found.");
            }
        }

        // POST api/<EmployeeController>
        [HttpPost, Authorize, ValidateAntiForgeryToken]
        public IActionResult Post([FromBody] EmployeeApplicationUser employee)
        {
            try
            {
                //TO DO for later on: move the create logic for the employee position into a service 
                List<PositionApplicationRole> positions = iPositionRepository.GetPositions();
                if (employee.Position == null || employee.Position.Id == 0)
                {
                    employee.Position = positions.Where(x => x.Name == "No Position!").FirstOrDefault();
                }
                else
                {
                    PositionApplicationRole employeePositionFromDropDownList = positions.Where(x => x.Id == employee.Position.Id).FirstOrDefault();
                    employee.Position = employeePositionFromDropDownList;
                }
                iEmployeeRepository.CreateEmployee(employee);
                Uri uri = new Uri(Url.Link("GetEmployeeByID", new { Id = employee.Id }));
                iUnitOfWork.Save();
                return Created(uri, employee.Id.ToString());
            }
            catch (Exception ex)
            {
                return Content(ex.ToString(), BadRequest().ToString());
            }
        }

        // PUT api/<EmployeeController>/5
        [HttpPut("{id}"), Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
        public IActionResult Put(int id, [FromBody] UpdateEmployeeViewModel employeeViewModel)
        {
            if (employeeViewModel != null)
            {
                employeeViewModel.ID = id;
                if (string.IsNullOrEmpty(employeeViewModel.UserName))
                {
                    employeeViewModel.UserName = iEmployeeRepository.GetEmployeeByID(employeeViewModel.ID).UserName;
                }
                //TO DO for later on: move the update logic for the employee position into a service
                List<PositionApplicationRole> positions = iPositionRepository.GetPositions();
                if (employeeViewModel.Position.Id == 0)
                {
                    employeeViewModel.Position = iEmployeeRepository.GetEmployeeByID(employeeViewModel.ID).Position;
                }
                else
                {
                    PositionApplicationRole employeePositionFromDropDownList = positions.Where(x => x.Id == employeeViewModel.Position.Id).FirstOrDefault();
                    employeeViewModel.Position = employeePositionFromDropDownList;
                }
                EmployeeApplicationUser employee = new EmployeeApplicationUser()
                {
                    Id = employeeViewModel.ID,
                    UserName = employeeViewModel.UserName,
                    Position = employeeViewModel.Position
                };
                iEmployeeRepository.UpdateEmployee(employee);
                iUnitOfWork.Save();
                return Ok(employee);
            }
            else
            {
                return NotFound("Employee with ID " + id.ToString() + " was not found.");
            }
        }


        // DELETE api/<EmployeeController>/5
        [HttpDelete("{id}"), Authorize, ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            EmployeeApplicationUser employeeToDelete = iEmployeeRepository.GetEmployeeByID(id);
            if (employeeToDelete != null)
            {
                //if a certain reservation has this employee(the employee for deletion), we are setting its employee to be "NoEmployee"
                employeeToDelete.Reservations.ForEach(x =>
                    x.Employee = iEmployeeRepository.GetEmployees().Where(x => x.UserName == "NoEmployee").FirstOrDefault());
                iEmployeeRepository.DeleteEmployee(employeeToDelete.Id);
                iUnitOfWork.Save();
                return Ok(employeeToDelete);
            }
            else
            {
                return NotFound("Employee with ID " + id.ToString() + " was not found.");
            }
        }
    }
}